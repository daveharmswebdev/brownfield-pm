namespace PropertyManager.Application.Common.Interfaces;

/// <summary>
/// Interface for identity operations (user creation, email verification, etc.).
/// Implementation in Infrastructure layer using ASP.NET Core Identity.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Creates a new user with the given credentials and links to account.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <param name="accountId">Account to link user to</param>
    /// <param name="role">User's role (e.g., "Owner")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="emailConfirmed">Whether email should be marked as confirmed (true for invited users)</param>
    /// <returns>Tuple of (UserId, PasswordErrors). UserId is null if creation failed.</returns>
    Task<(Guid? UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string email,
        string password,
        Guid accountId,
        string role,
        CancellationToken cancellationToken = default,
        bool emailConfirmed = false);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an email verification token for the user.
    /// </summary>
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the email using the provided token.
    /// </summary>
    /// <returns>Tuple of (Success, ErrorMessage).</returns>
    Task<(bool Success, string? ErrorMessage)> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates user credentials for login.
    /// Checks email exists, password is correct, and email is verified.
    /// </summary>
    /// <returns>Tuple of (Success, UserId, AccountId, Role, ErrorMessage).</returns>
    Task<(bool Success, Guid? UserId, Guid? AccountId, string? Role, string? ErrorMessage)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user ID by email address (case-insensitive lookup).
    /// Used for password reset flow where we need the user ID but don't want to reveal if user exists.
    /// </summary>
    /// <returns>UserId if found, null otherwise.</returns>
    Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password reset token for the user.
    /// Token is cryptographically secure and valid for 1 hour (AC6.2).
    /// </summary>
    /// <returns>Encoded token containing userId and reset token.</returns>
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user password using the provided token.
    /// Validates token is not expired or used (AC6.3, AC6.5).
    /// </summary>
    /// <returns>Tuple of (Success, ErrorMessage).</returns>
    Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
