using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Income entity linked to a property.
/// </summary>
public class Income : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid AccountId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Source { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public Property Property { get; set; } = null!;
    // Note: CreatedByUserId references ApplicationUser (Identity) - navigation configured in Infrastructure
}
