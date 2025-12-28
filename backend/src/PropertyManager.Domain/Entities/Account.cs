namespace PropertyManager.Domain.Entities;

/// <summary>
/// Tenant boundary - all tenant data is filtered by AccountId.
/// </summary>
public class Account
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    // Note: Users are managed via ApplicationUser (Identity) in Infrastructure layer
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Income { get; set; } = new List<Income>();
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
