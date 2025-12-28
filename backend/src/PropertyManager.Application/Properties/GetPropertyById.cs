using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Properties;

/// <summary>
/// Query to get a single property by ID for the current user's account.
/// Returns null if property doesn't exist or belongs to different account (AC-2.3.6).
/// </summary>
/// <param name="Id">Property GUID</param>
/// <param name="Year">Optional tax year filter (defaults to current year) (AC-3.5.6)</param>
public record GetPropertyByIdQuery(Guid Id, int? Year = null) : IRequest<PropertyDetailDto?>;

/// <summary>
/// Detail DTO for property view page (AC-2.3.2).
/// Extends PropertySummaryDto with createdAt, updatedAt, and recent activity.
/// </summary>
public record PropertyDetailDto(
    Guid Id,
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode,
    decimal ExpenseTotal,
    decimal IncomeTotal,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ExpenseSummaryDto> RecentExpenses,
    IReadOnlyList<IncomeSummaryDto> RecentIncome
);

/// <summary>
/// Placeholder DTO for recent expenses (implemented in Epic 3).
/// </summary>
public record ExpenseSummaryDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date
);

/// <summary>
/// Placeholder DTO for recent income (implemented in Epic 4).
/// </summary>
public record IncomeSummaryDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date
);

/// <summary>
/// Handler for GetPropertyByIdQuery.
/// Returns property details for the current user's account only (AC-2.3.5, AC-2.3.6).
/// </summary>
public class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDetailDto?>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetPropertyByIdQueryHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<PropertyDetailDto?> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        // Use provided year or default to current year (AC-3.5.6)
        var year = request.Year ?? DateTime.UtcNow.Year;
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        var property = await _dbContext.Properties
            .Where(p => p.Id == request.Id && p.AccountId == _currentUser.AccountId && p.DeletedAt == null)
            .Select(p => new PropertyDetailDto(
                p.Id,
                p.Name,
                p.Street,
                p.City,
                p.State,
                p.ZipCode,
                _dbContext.Expenses
                    .Where(e => e.PropertyId == p.Id
                        && e.AccountId == _currentUser.AccountId
                        && e.DeletedAt == null
                        && e.Date >= yearStart && e.Date <= yearEnd)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                _dbContext.Income
                    .Where(i => i.PropertyId == p.Id
                        && i.AccountId == _currentUser.AccountId
                        && i.DeletedAt == null
                        && i.Date >= yearStart && i.Date <= yearEnd)
                    .Sum(i => (decimal?)i.Amount) ?? 0m,
                p.CreatedAt,
                p.UpdatedAt,
                _dbContext.Expenses
                    .Where(e => e.PropertyId == p.Id
                        && e.AccountId == _currentUser.AccountId
                        && e.DeletedAt == null
                        && e.Date >= yearStart && e.Date <= yearEnd)
                    .OrderByDescending(e => e.Date)
                    .Take(5)
                    .Select(e => new ExpenseSummaryDto(
                        e.Id,
                        e.Description ?? string.Empty,
                        e.Amount,
                        new DateTime(e.Date.Year, e.Date.Month, e.Date.Day)
                    ))
                    .ToList(),
                _dbContext.Income
                    .Where(i => i.PropertyId == p.Id
                        && i.AccountId == _currentUser.AccountId
                        && i.DeletedAt == null
                        && i.Date >= yearStart && i.Date <= yearEnd)
                    .OrderByDescending(i => i.Date)
                    .Take(5)
                    .Select(i => new IncomeSummaryDto(
                        i.Id,
                        i.Description ?? string.Empty,
                        i.Amount,
                        new DateTime(i.Date.Year, i.Date.Month, i.Date.Day)
                    ))
                    .ToList()
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return property;
    }
}
