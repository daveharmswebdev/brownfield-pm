using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query to check for potential duplicate expenses (AC-3.6.1, AC-3.6.5).
/// Duplicate detection criteria: Same PropertyId + Amount + Date within 24 hours (±1 day).
/// </summary>
public record CheckDuplicateExpenseQuery(
    Guid PropertyId,
    decimal Amount,
    DateOnly Date
) : IRequest<DuplicateCheckResult>;

/// <summary>
/// Result of duplicate expense check.
/// </summary>
public record DuplicateCheckResult(
    bool IsDuplicate,
    DuplicateExpenseDto? ExistingExpense
);

/// <summary>
/// DTO for existing expense details shown in duplicate warning dialog (AC-3.6.2).
/// Includes only the fields needed for the warning message.
/// </summary>
public record DuplicateExpenseDto(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string? Description
);

/// <summary>
/// Handler for CheckDuplicateExpenseQuery.
/// Returns potential duplicate expense if found within the date window.
/// Uses global query filter for AccountId isolation.
/// Excludes soft-deleted expenses (DeletedAt != null).
/// </summary>
public class CheckDuplicateExpenseHandler : IRequestHandler<CheckDuplicateExpenseQuery, DuplicateCheckResult>
{
    private readonly IAppDbContext _dbContext;

    public CheckDuplicateExpenseHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DuplicateCheckResult> Handle(CheckDuplicateExpenseQuery request, CancellationToken cancellationToken)
    {
        // Calculate date window: ±1 day (within 24 hours)
        // AC-3.6.5: Same day or day before/after = duplicate
        // Edge case: Dec 1 vs Dec 2 = duplicate, Dec 1 vs Dec 3 = no duplicate
        var startDate = request.Date.AddDays(-1);
        var endDate = request.Date.AddDays(1);

        // Query for potential duplicate
        // Global query filter handles AccountId isolation
        // DeletedAt filter handled by global query filter as well
        var existingExpense = await _dbContext.Expenses
            .Where(e => e.PropertyId == request.PropertyId)
            .Where(e => e.Amount == request.Amount)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .Select(e => new DuplicateExpenseDto(
                e.Id,
                e.Date,
                e.Amount,
                e.Description
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (existingExpense != null)
        {
            return new DuplicateCheckResult(
                IsDuplicate: true,
                ExistingExpense: existingExpense
            );
        }

        return new DuplicateCheckResult(
            IsDuplicate: false,
            ExistingExpense: null
        );
    }
}
