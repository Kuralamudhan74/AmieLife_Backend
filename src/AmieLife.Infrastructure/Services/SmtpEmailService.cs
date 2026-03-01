using AmieLife.Application.Common.Interfaces;
using AmieLife.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AmieLife.Infrastructure.Services;

/// <summary>
/// Real SMTP email service using MailKit.
/// Works with any SMTP provider: Brevo, Mailtrap, Gmail, SendGrid SMTP, AWS SES SMTP.
/// Registered when Smtp:Enabled is true in configuration.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpSettings> settings,
        IConfiguration configuration,
        IEmailTemplateService templateService,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _configuration = configuration;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";

        var htmlBody = _templateService.RenderEmailVerification(verifyUrl);

        await SendEmailAsync(
            toEmail: toEmail,
            subject: "Verify your AmieLife account",
            htmlBody: htmlBody,
            ct: ct);

        _logger.LogInformation("Verification email sent to {Email}", toEmail);
    }

    public async Task SendPasswordResetAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        var htmlBody = _templateService.RenderPasswordReset(resetUrl);

        await SendEmailAsync(
            toEmail: toEmail,
            subject: "Reset your AmieLife password",
            htmlBody: htmlBody,
            ct: ct);

        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = HtmlToPlainText(htmlBody)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = _settings.TimeoutMs;

        var secureOption = DetermineSecurityOption();

        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, ct);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            }

            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP {Host}:{Port}",
                toEmail, _settings.Host, _settings.Port);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, ct);
            }
        }
    }

    private SecureSocketOptions DetermineSecurityOption()
    {
        if (!_settings.UseSsl) return SecureSocketOptions.None;
        return _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static string HtmlToPlainText(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }
}
