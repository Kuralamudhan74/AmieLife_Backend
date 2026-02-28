namespace AmieLife.Application.DTOs.Auth;

public record SignupRequestDto(
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);
