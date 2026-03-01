using System.Reflection;
using AmieLife.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace AmieLife.Infrastructure.Services;

/// <summary>
/// Renders HTML email templates from embedded resources.
/// Templates use simple placeholder substitution: {{VARIABLE_NAME}}.
/// Registered as Singleton — templates are loaded once at startup and reused.
/// </summary>
public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly string _verificationTemplate;
    private readonly string _passwordResetTemplate;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _verificationTemplate = LoadEmbeddedTemplate("EmailVerification.html");
        _passwordResetTemplate = LoadEmbeddedTemplate("PasswordReset.html");

        logger.LogInformation("Email templates loaded successfully from embedded resources.");
    }

    public string RenderEmailVerification(string verifyUrl)
    {
        return _verificationTemplate
            .Replace("{{VERIFY_URL}}", verifyUrl)
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString());
    }

    public string RenderPasswordReset(string resetUrl)
    {
        return _passwordResetTemplate
            .Replace("{{RESET_URL}}", resetUrl)
            .Replace("{{EXPIRY_MINUTES}}", AppConstants.Auth.PasswordResetTokenExpirationMinutes.ToString())
            .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString());
    }

    private static string LoadEmbeddedTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"AmieLife.Infrastructure.Templates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded email template '{resourceName}' not found. " +
                $"Ensure the file exists at Templates/{templateName} and is marked as EmbeddedResource.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
