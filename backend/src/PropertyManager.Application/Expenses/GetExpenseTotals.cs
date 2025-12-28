using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query to get expense totals for a given year with per-property breakdown (AC-3.5.2, AC-3.5.4).
/// </summary>
public record GetExpenseTotalsQuery(int Year) : IRequest<ExpenseTotalsDto>;

/// <summary>
/// DTO for expense totals response.
/// </summary>
public record ExpenseTotalsDto(
    decimal TotalExpenses,
    int Year,
    List<PropertyExpenseTotal> ByProperty
);

/// <summary>
/// DTO for per-property expense total.
/// </summary>
public record PropertyExpenseTotal(
    Guid PropertyId,
    string PropertyName,
    decimal Total
);

/// <summary>
/// Handler for GetExpenseTotalsQuery.
/// Returns total expenses and per-property breakdown for a given year.
/// Uses global query filter for AccountId isolation.
/// </summary>
public class GetExpenseTotalsHandler : IRequestHandler<GetExpenseTotalsQuery, ExpenseTotalsDto>
{
    private readonly IAppDbContext _dbContext;

    public GetExpenseTotalsHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExpenseTotalsDto> Handle(GetExpenseTotalsQuery request, CancellationToken cancellationToken)
    {
        // Calculate year date range
        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        // Get per-property totals with grouping
        var propertyTotals = await _dbContext.Expenses
            .Where(e => e.Date >= yearStart && e.Date <= yearEnd)
            .GroupBy(e => new { e.PropertyId, e.Property.Name })
            .Select(g => new PropertyExpenseTotal(
                g.Key.PropertyId,
                g.Key.Name,
                g.Sum(e => e.Amount)
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Calculate overall total
        var totalExpenses = propertyTotals.Sum(p => p.Total);

        return new ExpenseTotalsDto(
            TotalExpenses: totalExpenses,
            Year: request.Year,
            ByProperty: propertyTotals
        );
    }
}
