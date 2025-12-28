using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Invitation entity for invite-only registration.
/// Stores hashed tokens for security.
/// </summary>
public class Invitation : AuditableEntity, ITenantEntity
{
    /// <summary>
    /// Account ID for tenant isolation (from ITenantEntity).
    /// The account of the user who sent the invitation.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Email address of the invited user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The hashed invitation token value.
    /// Only the hash is stored for security - raw token sent in email only.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// The user ID of who sent this invitation.
    /// </summary>
    public Guid InvitedByUserId { get; set; }

    /// <summary>
    /// When this invitation expires (24 hours from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this invitation was accepted (null if not yet accepted).
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Whether this invitation is still valid (not expired and not accepted).
    /// </summary>
    public bool IsValid => AcceptedAt == null && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Whether this invitation has already been used.
    /// </summary>
    public bool IsAccepted => AcceptedAt != null;

    /// <summary>
    /// Whether this invitation has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
}
