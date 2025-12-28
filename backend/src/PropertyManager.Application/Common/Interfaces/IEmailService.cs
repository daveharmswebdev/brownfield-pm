namespace PropertyManager.Application.Common.Interfaces;

/// <summary>
/// Interface for email operations.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a verification email with the provided token.
    /// </summary>
    Task SendVerificationEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email with the provided token (AC6.6).
    /// Link format: {frontend_url}/reset-password?token={token}
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
}
