using AmieLife.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmieLife.Infrastructure.Services;

/// <summary>
/// STUB email service. Logs the token to the console for local development.
/// Replace this with SendGrid / SMTP / AWS SES in production.
///
/// To implement real email:
/// 1. Install SendGrid NuGet package
/// 2. Inject IConfiguration to read Smtp:ApiKey / SendGrid:ApiKey
/// 3. Build HTML email templates
/// 4. Send via the provider SDK
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task SendEmailVerificationAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

        // In production replace with real email dispatch
        _logger.LogInformation(
            "[EMAIL STUB] Verification email to {Email}. URL: {VerifyUrl}",
            toEmail, verifyUrl);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        _logger.LogInformation(
            "[EMAIL STUB] Password reset email to {Email}. URL: {ResetUrl}",
            toEmail, resetUrl);

        return Task.CompletedTask;
    }
}
