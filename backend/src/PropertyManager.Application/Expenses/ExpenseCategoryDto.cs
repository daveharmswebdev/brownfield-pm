namespace PropertyManager.Application.Expenses;

/// <summary>
/// DTO for expense category data (AC-3.1.4).
/// </summary>
public record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string? ScheduleELine,
    int SortOrder
);
