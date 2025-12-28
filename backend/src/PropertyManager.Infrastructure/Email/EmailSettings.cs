namespace PropertyManager.Infrastructure.Email;

/// <summary>
/// Configuration settings for email service.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 1025;

    /// <summary>
    /// SMTP username (optional for MailHog).
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password (optional for MailHog).
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Enable SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// Sender email address.
    /// </summary>
    public string FromEmail { get; set; } = "noreply@propertymanager.local";

    /// <summary>
    /// Sender display name.
    /// </summary>
    public string FromName { get; set; } = "Property Manager";

    /// <summary>
    /// Base URL for verification links.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
