using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Refresh token entity for JWT authentication.
/// Stored in database for validation and revocation support.
/// </summary>
public class RefreshToken : AuditableEntity, ITenantEntity
{
    /// <summary>
    /// The user this refresh token belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Account ID for tenant isolation (from ITenantEntity).
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// The hashed refresh token value.
    /// Only the hash is stored for security.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When this refresh token expires (7 days from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this token was revoked (null if still valid).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address or device identifier for the session.
    /// Supports multiple concurrent sessions (AC4.7).
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Whether this token is still valid (not expired and not revoked).
    /// </summary>
    public bool IsValid => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
