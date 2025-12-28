using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Dashboard;

/// <summary>
/// Query to get dashboard totals for the current user's account (AC-4.4.1, AC-4.4.2).
/// </summary>
/// <param name="Year">Tax year to aggregate totals for</param>
public record GetDashboardTotalsQuery(int Year) : IRequest<DashboardTotalsDto>;

/// <summary>
/// Dashboard totals DTO containing aggregated financial data.
/// </summary>
public record DashboardTotalsDto(
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal NetIncome,
    int PropertyCount
);

/// <summary>
/// Handler for GetDashboardTotalsQuery.
/// Aggregates all expenses and income for the year across the account.
/// </summary>
public class GetDashboardTotalsQueryHandler : IRequestHandler<GetDashboardTotalsQuery, DashboardTotalsDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetDashboardTotalsQueryHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<DashboardTotalsDto> Handle(GetDashboardTotalsQuery request, CancellationToken cancellationToken)
    {
        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        // Get total expenses for the year
        var totalExpenses = await _dbContext.Expenses
            .Where(e => e.AccountId == _currentUser.AccountId
                && e.DeletedAt == null
                && e.Date >= yearStart && e.Date <= yearEnd)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        // Get total income for the year
        var totalIncome = await _dbContext.Income
            .Where(i => i.AccountId == _currentUser.AccountId
                && i.DeletedAt == null
                && i.Date >= yearStart && i.Date <= yearEnd)
            .SumAsync(i => (decimal?)i.Amount, cancellationToken) ?? 0m;

        // Get property count (active properties only)
        var propertyCount = await _dbContext.Properties
            .Where(p => p.AccountId == _currentUser.AccountId && p.DeletedAt == null)
            .CountAsync(cancellationToken);

        // Calculate net income (Income - Expenses)
        var netIncome = totalIncome - totalExpenses;

        return new DashboardTotalsDto(
            totalExpenses,
            totalIncome,
            netIncome,
            propertyCount
        );
    }
}
