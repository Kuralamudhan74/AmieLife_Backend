namespace AmieLife.Infrastructure.Options;

/// <summary>
/// Strongly-typed SMTP configuration. Bound from appsettings "Smtp" section.
/// When Enabled is false, the console stub ConsoleEmailService is used instead.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = "noreply@amielife.com";
    public string FromName { get; set; } = "AmieLife";
    public int TimeoutMs { get; set; } = 10_000;
}
