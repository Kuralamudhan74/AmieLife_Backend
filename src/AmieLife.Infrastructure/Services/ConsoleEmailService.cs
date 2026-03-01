using AmieLife.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmieLife.Infrastructure.Services;

/// <summary>
/// Console-only email service for local development when SMTP is not configured.
/// Logs verification/reset URLs to the console — no emails are sent.
/// Used when Smtp:Enabled is false (default).
/// </summary>
public sealed class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly IConfiguration _configuration;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task SendEmailVerificationAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

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
