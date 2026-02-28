using AmieLife.Application.Common.Models;
using AmieLife.Application.DTOs.Auth;

namespace AmieLife.Application.Common.Interfaces;

/// <summary>
/// Defines all authentication use cases. No implementation details here.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user, sends a verification email stub, returns tokens.</summary>
    Task<Result<AuthResponseDto>> SignupAsync(SignupRequestDto request, CancellationToken ct = default);

    /// <summary>Validates credentials and issues access + refresh tokens.</summary>
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

    /// <summary>Validates a refresh token, rotates it, returns new token pair.</summary>
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);

    /// <summary>Revokes the supplied refresh token (single-device logout).</summary>
    Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default);

    /// <summary>Validates email verification token and marks the user as verified.</summary>
    Task<Result> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken ct = default);

    /// <summary>Generates a password reset token and returns it (email sending is a stub).</summary>
    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);

    /// <summary>Validates reset token, updates password, revokes all refresh tokens.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);

    /// <summary>Creates a guest user record for checkout flows.</summary>
    Task<Result<GuestUserResponseDto>> CreateGuestUserAsync(GuestUserRequestDto request, CancellationToken ct = default);
}
