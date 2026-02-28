namespace AmieLife.Application.DTOs.Auth;

public record ResetPasswordRequestDto(
    string Token,
    string NewPassword,
    string ConfirmNewPassword
);
