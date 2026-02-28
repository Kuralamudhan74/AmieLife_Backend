using AmieLife.Application.Common.Interfaces;
using AmieLife.Application.Common.Models;
using AmieLife.Application.DTOs.Auth;
using AmieLife.Domain.Entities;
using AmieLife.Domain.Enums;
using AmieLife.Shared.Constants;
using AmieLife.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace AmieLife.Application.Services;

/// <summary>
/// Orchestrates all authentication use cases.
/// Contains ONLY business logic — no infrastructure concerns here.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IEmailVerificationTokenRepository _emailVerificationRepo;
    private readonly IPasswordResetTokenRepository _passwordResetRepo;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IEmailVerificationTokenRepository emailVerificationRepo,
        IPasswordResetTokenRepository passwordResetRepo,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _emailVerificationRepo = emailVerificationRepo;
        _passwordResetRepo = passwordResetRepo;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponseDto>> SignupAsync(SignupRequestDto request, CancellationToken ct = default)
    {
        if (await _userRepo.EmailExistsAsync(request.Email, ct))
            return Result<AuthResponseDto>.Failure("An account with this email already exists.");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, AppConstants.Auth.BcryptWorkFactor),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Customer,
            IsEmailVerified = false,
            IsGuest = false,
            Status = UserStatus.Active
        };

        await _userRepo.AddAsync(user, ct);
        _logger.LogInformation("New user registered: {UserId}", user.UserId);

        // Generate and store email verification token
        var rawToken = HashHelper.GenerateSecureToken();
        var tokenHash = HashHelper.HashToken(rawToken);
        var verificationToken = new EmailVerificationToken
        {
            TokenId = Guid.NewGuid(),
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(AppConstants.Auth.EmailVerificationTokenExpirationHours)
        };
        await _emailVerificationRepo.AddAsync(verificationToken, ct);

        // Email sending: stub for now — replace with real implementation
        await _emailService.SendEmailVerificationAsync(user.Email, rawToken, ct);

        // Do NOT return tokens — user must verify email first
        return Result<AuthResponseDto>.Failure(
            "Registration successful. Please check your email to verify your account before logging in.");
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        // Generic message — do not reveal whether the email exists
        const string genericError = "Invalid email or password.";

        if (user is null || user.IsGuest)
            return Result<AuthResponseDto>.Failure(genericError);

        if (user.Status == UserStatus.Suspended)
            return Result<AuthResponseDto>.Failure("This account has been suspended. Please contact support.");

        if (user.Status == UserStatus.Deleted)
            return Result<AuthResponseDto>.Failure(genericError);

        if (user.IsLockedOut())
        {
            _logger.LogWarning("Login attempt on locked account: {UserId}", user.UserId);
            return Result<AuthResponseDto>.Failure(
                $"Account is temporarily locked due to multiple failed attempts. Try again after {user.LockoutEndTime:HH:mm UTC}.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(AppConstants.Auth.MaxFailedLoginAttempts, AppConstants.Auth.LockoutDurationMinutes);
            await _userRepo.UpdateAsync(user, ct);
            _logger.LogWarning("Failed login attempt for user: {UserId}", user.UserId);
            return Result<AuthResponseDto>.Failure(genericError);
        }

        if (!user.IsEmailVerified)
            return Result<AuthResponseDto>.Failure("Please verify your email address before logging in.");

        user.RecordSuccessfulLogin();
        await _userRepo.UpdateAsync(user, ct);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateAndStoreRefreshTokenAsync(user.UserId, ct);

        _logger.LogInformation("User logged in successfully: {UserId}", user.UserId);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresInSeconds: AppConstants.Auth.AccessTokenExpirationMinutes * 60
        ));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive())
        {
            _logger.LogWarning("Refresh token attempt with invalid/expired/revoked token.");
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");
        }

        var user = await _userRepo.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || user.Status != UserStatus.Active)
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");

        // Rotate: revoke the used token
        await _refreshTokenRepo.RevokeAsync(storedToken, ct);

        // Issue fresh pair
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await _tokenService.GenerateAndStoreRefreshTokenAsync(user.UserId, ct);

        _logger.LogInformation("Refresh token rotated for user: {UserId}", user.UserId);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresInSeconds: AppConstants.Auth.AccessTokenExpirationMinutes * 60
        ));
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken is not null && storedToken.IsActive())
        {
            await _refreshTokenRepo.RevokeAsync(storedToken, ct);
            _logger.LogInformation("User logged out, refresh token revoked for user: {UserId}", storedToken.UserId);
        }

        // Return success even if token was already invalid (prevent token-probing)
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.HashToken(request.Token);
        var token = await _emailVerificationRepo.GetByTokenHashAsync(tokenHash, ct);

        if (token is null || !token.IsValid())
            return Result.Failure("The verification link is invalid or has expired.");

        token.IsUsed = true;
        await _emailVerificationRepo.UpdateAsync(token, ct);

        var user = await _userRepo.GetByIdAsync(token.UserId, ct);
        if (user is null)
            return Result.Failure("User not found.");

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);

        _logger.LogInformation("Email verified for user: {UserId}", user.UserId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();
        var user = await _userRepo.GetByEmailAsync(email, ct);

        // Always return success — do not reveal whether the email exists
        if (user is null || user.IsGuest || !user.IsEmailVerified)
        {
            _logger.LogInformation("Forgot password requested for non-existent/unverified email: {Email}", email);
            return Result<string>.Success("If an account with that email exists, a reset link has been sent.");
        }

        var rawToken = HashHelper.GenerateSecureToken();
        var tokenHash = HashHelper.HashToken(rawToken);
        var resetToken = new PasswordResetToken
        {
            ResetId = Guid.NewGuid(),
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.Auth.PasswordResetTokenExpirationMinutes)
        };

        await _passwordResetRepo.AddAsync(resetToken, ct);
        await _emailService.SendPasswordResetAsync(user.Email, rawToken, ct);

        _logger.LogInformation("Password reset token generated for user: {UserId}", user.UserId);
        return Result<string>.Success("If an account with that email exists, a reset link has been sent.");
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        var tokenHash = HashHelper.HashToken(request.Token);
        var resetToken = await _passwordResetRepo.GetByTokenHashAsync(tokenHash, ct);

        if (resetToken is null || !resetToken.IsValid())
            return Result.Failure("The password reset link is invalid or has expired.");

        var user = await _userRepo.GetByIdAsync(resetToken.UserId, ct);
        if (user is null)
            return Result.Failure("User not found.");

        // Mark token used first to prevent race conditions
        resetToken.IsUsed = true;
        await _passwordResetRepo.UpdateAsync(resetToken, ct);

        // Update password and revoke all sessions
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, AppConstants.Auth.BcryptWorkFactor);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);

        // Revoke all refresh tokens — forces re-authentication on all devices
        await _refreshTokenRepo.RevokeAllForUserAsync(user.UserId, ct);

        _logger.LogInformation("Password reset completed for user: {UserId}", user.UserId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<GuestUserResponseDto>> CreateGuestUserAsync(GuestUserRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();

        // If a guest already exists with this email, return a fresh access token
        var existing = await _userRepo.GetByEmailAsync(email, ct);
        if (existing is not null && !existing.IsGuest)
            return Result<GuestUserResponseDto>.Failure(
                "An account with this email already exists. Please log in.");

        User guestUser;
        if (existing is not null && existing.IsGuest)
        {
            guestUser = existing;
        }
        else
        {
            guestUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                PasswordHash = null,
                IsGuest = true,
                Role = UserRole.Customer,
                IsEmailVerified = false,
                Status = UserStatus.Active
            };
            await _userRepo.AddAsync(guestUser, ct);
            _logger.LogInformation("Guest user created: {UserId}", guestUser.UserId);
        }

        var guestAccessToken = _tokenService.GenerateAccessToken(guestUser);

        return Result<GuestUserResponseDto>.Success(new GuestUserResponseDto(
            UserId: guestUser.UserId,
            Email: guestUser.Email,
            GuestAccessToken: guestAccessToken
        ));
    }
}
