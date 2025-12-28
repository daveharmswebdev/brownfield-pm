namespace PropertyManager.Application.Income;

/// <summary>
/// DTO for income data (AC-4.1.6).
/// </summary>
public record IncomeDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    decimal Amount,
    DateOnly Date,
    string? Source,
    string? Description,
    DateTime CreatedAt
);
