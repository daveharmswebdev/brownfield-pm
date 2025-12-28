using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Expense entity linked to a property and expense category.
/// </summary>
public class Expense : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid AccountId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public Property Property { get; set; } = null!;
    public ExpenseCategory Category { get; set; } = null!;
    public Receipt? Receipt { get; set; }
    // Note: CreatedByUserId references ApplicationUser (Identity) - navigation configured in Infrastructure
}
