using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Infrastructure.Email;

/// <summary>
/// SMTP implementation of IEmailService.
/// Uses MailHog for local development.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_settings.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        var subject = "Verify your email address - Property Manager";
        var htmlBody = GenerateVerificationEmailHtml(verificationUrl);
        var textBody = GenerateVerificationEmailText(verificationUrl);

        await SendEmailAsync(email, subject, htmlBody, textBody, cancellationToken);

        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    private async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);

        client.EnableSsl = _settings.EnableSsl;

        if (!string.IsNullOrEmpty(_settings.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
        message.To.Add(new MailAddress(to));
        message.Subject = subject;
        message.IsBodyHtml = true;
        message.Body = htmlBody;

        // Add plain text alternative
        var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        message.AlternateViews.Add(plainTextView);
        message.AlternateViews.Add(htmlView);

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string GenerateVerificationEmailHtml(string verificationUrl)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify your email</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #66BB6A; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;"">
        <h1 style=""color: white; margin: 0;"">Property Manager</h1>
    </div>
    <div style=""background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px;"">
        <h2 style=""color: #333; margin-top: 0;"">Verify your email address</h2>
        <p>Thank you for registering with Property Manager. Please click the button below to verify your email address and complete your registration.</p>
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{verificationUrl}"" style=""background-color: #66BB6A; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;"">Verify Email</a>
        </div>
        <p style=""color: #666; font-size: 14px;"">This link will expire in 24 hours.</p>
        <p style=""color: #666; font-size: 14px;"">If you did not create an account, you can safely ignore this email.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 20px 0;"">
        <p style=""color: #999; font-size: 12px;"">If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""color: #999; font-size: 12px; word-break: break-all;"">{verificationUrl}</p>
    </div>
</body>
</html>";
    }

    private static string GenerateVerificationEmailText(string verificationUrl)
    {
        return $@"Property Manager - Verify your email address

Thank you for registering with Property Manager. Please click the link below to verify your email address and complete your registration.

Verify your email: {verificationUrl}

This link will expire in 24 hours.

If you did not create an account, you can safely ignore this email.";
    }

    /// <summary>
    /// Sends a password reset email (AC6.6).
    /// </summary>
    public async Task SendPasswordResetEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_settings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var subject = "Reset your password - Property Manager";
        var htmlBody = GeneratePasswordResetEmailHtml(resetUrl);
        var textBody = GeneratePasswordResetEmailText(resetUrl);

        await SendEmailAsync(email, subject, htmlBody, textBody, cancellationToken);

        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    private static string GeneratePasswordResetEmailHtml(string resetUrl)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset your password</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #66BB6A; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;"">
        <h1 style=""color: white; margin: 0;"">Property Manager</h1>
    </div>
    <div style=""background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px;"">
        <h2 style=""color: #333; margin-top: 0;"">Reset your password</h2>
        <p>We received a request to reset your password. Click the button below to create a new password.</p>
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{resetUrl}"" style=""background-color: #66BB6A; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;"">Reset Password</a>
        </div>
        <p style=""color: #666; font-size: 14px;"">This link will expire in 1 hour.</p>
        <p style=""color: #d9534f; font-size: 14px;""><strong>Security notice:</strong> If you didn't request this password reset, you can safely ignore this email. Your password will remain unchanged.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 20px 0;"">
        <p style=""color: #999; font-size: 12px;"">If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""color: #999; font-size: 12px; word-break: break-all;"">{resetUrl}</p>
    </div>
</body>
</html>";
    }

    private static string GeneratePasswordResetEmailText(string resetUrl)
    {
        return $@"Property Manager - Reset your password

We received a request to reset your password. Click the link below to create a new password.

Reset your password: {resetUrl}

This link will expire in 1 hour.

SECURITY NOTICE: If you didn't request this password reset, you can safely ignore this email. Your password will remain unchanged.";
    }
}
