namespace AmieLife.Infrastructure.Services;

/// <summary>
/// Infrastructure service for rendering HTML email templates.
/// Lives in Infrastructure layer — not exposed to Application layer (dependency direction prevents it).
/// </summary>
public interface IEmailTemplateService
{
    string RenderEmailVerification(string verifyUrl);
    string RenderPasswordReset(string resetUrl);
}
