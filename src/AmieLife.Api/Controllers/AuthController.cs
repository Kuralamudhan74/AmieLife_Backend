using AmieLife.Application.Common.Interfaces;
using AmieLife.Application.DTOs.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmieLife.Api.Controllers;

/// <summary>
/// Authentication endpoints. No business logic lives here —
/// the controller only delegates to IAuthService and formats responses.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user account.</summary>
    /// <response code="200">Registration successful, verification email sent.</response>
    /// <response code="400">Validation failure or email already exists.</response>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Signup(
        [FromBody] SignupRequestDto request,
        [FromServices] IValidator<SignupRequestDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.ToDictionary()));

        var result = await _authService.SignupAsync(request, ct);

        // Signup returns a "success with message" pattern (user must verify email)
        return Ok(new MessageResponseDto(result.Error ?? result.Data?.AccessToken ?? "Registration received."));
    }

    /// <summary>Login with email and password.</summary>
    /// <response code="200">Returns access and refresh tokens.</response>
    /// <response code="400">Invalid credentials, locked account, or unverified email.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        [FromServices] IValidator<LoginRequestDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await _authService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Login Failed",
                Detail = result.Error
            });

        return Ok(result.Data);
    }

    /// <summary>Issue a new token pair using a valid refresh token.</summary>
    /// <response code="200">Returns new access and refresh tokens.</response>
    /// <response code="401">Refresh token is invalid, expired, or revoked.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Refresh token is required." });

        var result = await _authService.RefreshTokenAsync(request, ct);
        if (!result.IsSuccess)
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Token Refresh Failed",
                Detail = result.Error
            });

        return Ok(result.Data);
    }

    /// <summary>Logout and revoke the refresh token for the current device.</summary>
    /// <response code="200">Logout successful.</response>
    [HttpPost("logout")]
    [AllowAnonymous] // Logout should succeed even with expired access token
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return Ok(new MessageResponseDto("Logged out successfully."));
    }

    /// <summary>Verify email address using a token from the verification email.</summary>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Token is invalid or expired.</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken ct)
    {
        var result = await _authService.VerifyEmailAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Verification Failed", Detail = result.Error });

        return Ok(new MessageResponseDto("Email verified successfully. You may now log in."));
    }

    /// <summary>Request a password reset email.</summary>
    /// <response code="200">Always returns success to prevent email enumeration.</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        var result = await _authService.ForgotPasswordAsync(request, ct);
        return Ok(new MessageResponseDto(result.Data ?? "If an account with that email exists, a reset link has been sent."));
    }

    /// <summary>Reset password using a valid reset token.</summary>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Token invalid/expired or passwords don't match.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        [FromServices] IValidator<ResetPasswordRequestDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await _authService.ResetPasswordAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Reset Failed", Detail = result.Error });

        return Ok(new MessageResponseDto("Password reset successfully. All sessions have been invalidated."));
    }

    /// <summary>Create a guest user for checkout without registration.</summary>
    /// <response code="200">Returns guest user ID and a short-lived access token.</response>
    /// <response code="400">Email belongs to a registered account.</response>
    [HttpPost("guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GuestUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGuest([FromBody] GuestUserRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Email is required." });

        var result = await _authService.CreateGuestUserAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Guest Creation Failed", Detail = result.Error });

        return Ok(result.Data);
    }
}

/// <summary>Simple message wrapper used by endpoints that return only a status message.</summary>
public record MessageResponseDto(string Message);
