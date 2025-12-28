using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Receipt entity for storing uploaded receipts.
/// Can be unassigned (PropertyId null) or linked to a property.
/// </summary>
public class Receipt : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid AccountId { get; set; }
    public Guid? PropertyId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public Guid? ExpenseId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public Property? Property { get; set; }
    public Expense? Expense { get; set; }
    // Note: CreatedByUserId references ApplicationUser (Identity) - navigation configured in Infrastructure
}
