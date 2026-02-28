using AmieLife.Domain.Entities;

namespace AmieLife.Application.Common.Interfaces;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a signed JWT access token containing UserId, Email, and Role claims.</summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Creates a cryptographically random refresh token, persists its hash to the DB,
    /// and returns the raw token value to be sent to the client.
    /// </summary>
    Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId, CancellationToken ct = default);
}
