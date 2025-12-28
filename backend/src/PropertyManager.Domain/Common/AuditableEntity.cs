namespace PropertyManager.Domain.Common;

/// <summary>
/// Base class for all entities with audit tracking.
/// Timestamps are auto-populated in SaveChangesAsync override.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
