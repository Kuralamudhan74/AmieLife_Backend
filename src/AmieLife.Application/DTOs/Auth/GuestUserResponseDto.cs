namespace AmieLife.Application.DTOs.Auth;

public record GuestUserResponseDto(
    Guid UserId,
    string Email,
    string GuestAccessToken
);
