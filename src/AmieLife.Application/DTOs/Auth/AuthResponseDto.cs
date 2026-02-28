namespace AmieLife.Application.DTOs.Auth;

/// <summary>
/// Returned after successful login or token refresh.
/// Clients should store AccessToken in memory and RefreshToken in HttpOnly cookie or secure storage.
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType = "Bearer"
);
