using PropertyManager.Domain.Common;

namespace PropertyManager.Domain.Entities;

/// <summary>
/// Property entity with soft delete support and tenant isolation.
/// Represents a rental property with address details.
/// </summary>
public class Property : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Income { get; set; } = new List<Income>();
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
