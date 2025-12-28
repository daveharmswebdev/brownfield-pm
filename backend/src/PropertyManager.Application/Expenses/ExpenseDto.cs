namespace PropertyManager.Application.Expenses;

/// <summary>
/// DTO for expense data (AC-3.1.7).
/// </summary>
public record ExpenseDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid CategoryId,
    string CategoryName,
    string? ScheduleELine,
    decimal Amount,
    DateOnly Date,
    string? Description,
    Guid? ReceiptId,
    DateTime CreatedAt
);
