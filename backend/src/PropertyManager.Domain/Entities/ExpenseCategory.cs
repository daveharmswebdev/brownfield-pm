namespace PropertyManager.Domain.Entities;

/// <summary>
/// Global expense category for IRS Schedule E line items.
/// No AccountId - these are seed data, not user-editable.
/// </summary>
public class ExpenseCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ScheduleELine { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
