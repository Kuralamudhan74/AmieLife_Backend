using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AmieLife.Application.Common.Interfaces;
using AmieLife.Domain.Entities;
using AmieLife.Shared.Constants;
using AmieLife.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AmieLife.Infrastructure.Services;

/// <summary>
/// Handles JWT generation and refresh token lifecycle.
/// JWT signing key is pulled from configuration — never hardcoded.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public TokenService(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepo)
    {
        _configuration = configuration;
        _refreshTokenRepo = refreshTokenRepo;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("is_guest", user.IsGuest.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var expiryMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes",
            AppConstants.Auth.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var rawToken = HashHelper.GenerateSecureToken();
        var tokenHash = HashHelper.HashToken(rawToken);

        var expiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays",
            AppConstants.Auth.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };

        await _refreshTokenRepo.AddAsync(refreshToken, ct);

        // Return the raw token to the client — hash stays in DB
        return rawToken;
    }
}
