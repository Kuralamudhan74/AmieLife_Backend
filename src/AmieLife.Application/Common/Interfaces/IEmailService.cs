namespace AmieLife.Application.Common.Interfaces;

/// <summary>
/// Email dispatch abstraction. Current stub implementation just logs.
/// Replace with SendGrid / SMTP / SES implementation in production.
/// </summary>
public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string rawToken, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string rawToken, CancellationToken ct = default);
}
