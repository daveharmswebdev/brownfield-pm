namespace PropertyManager.Application.Common.Interfaces;

/// <summary>
/// Interface for JWT token generation and validation.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT access token with the required claims (AC4.2).
    /// Claims: userId, accountId, role, exp (60 minutes from issue).
    /// </summary>
    /// <returns>Tuple of (AccessToken, ExpiresInSeconds).</returns>
    Task<(string AccessToken, int ExpiresIn)> GenerateAccessTokenAsync(
        Guid userId,
        Guid accountId,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and stores a refresh token for the user (AC4.6).
    /// Refresh token is valid for 7 days.
    /// </summary>
    /// <returns>The refresh token string.</returns>
    Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns the associated user info if valid.
    /// </summary>
    /// <returns>Tuple of (IsValid, UserId, AccountId, Role).</returns>
    Task<(bool IsValid, Guid? UserId, Guid? AccountId, string? Role)> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., password change).
    /// </summary>
    Task RevokeAllUserRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
