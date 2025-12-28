using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Income;

/// <summary>
/// Query to get all income across all properties with filtering (AC-4.3.1, AC-4.3.2, AC-4.3.3, AC-4.3.4).
/// No pagination - income volume is typically lower than expenses.
/// </summary>
public record GetAllIncomeQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? PropertyId,
    int? Year
) : IRequest<IncomeListResult>;

/// <summary>
/// Result containing income list with totals (AC-4.3.6).
/// </summary>
public record IncomeListResult(
    List<IncomeDto> Items,
    int TotalCount,
    decimal TotalAmount
);

/// <summary>
/// Handler for GetAllIncomeQuery.
/// Returns all income across properties with optional filtering by:
/// - Date range (DateFrom, DateTo) - inclusive
/// - Single property (PropertyId)
/// - Tax year
/// Results sorted by Date descending (newest first).
/// AccountId isolation enforced via EF Core global query filters.
/// </summary>
public class GetAllIncomeHandler : IRequestHandler<GetAllIncomeQuery, IncomeListResult>
{
    private readonly IAppDbContext _dbContext;

    public GetAllIncomeHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IncomeListResult> Handle(GetAllIncomeQuery request, CancellationToken cancellationToken)
    {
        // Start with base query - global query filter handles AccountId isolation
        var query = _dbContext.Income.AsQueryable();

        // Apply year filter (AC-4.3.2 - respects global tax year selector)
        if (request.Year.HasValue)
        {
            var yearStart = new DateOnly(request.Year.Value, 1, 1);
            var yearEnd = new DateOnly(request.Year.Value, 12, 31);
            query = query.Where(i => i.Date >= yearStart && i.Date <= yearEnd);
        }

        // Apply date range filter (AC-4.3.3) - inclusive
        if (request.DateFrom.HasValue)
        {
            query = query.Where(i => i.Date >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(i => i.Date <= request.DateTo.Value);
        }

        // Apply property filter (AC-4.3.4)
        if (request.PropertyId.HasValue)
        {
            query = query.Where(i => i.PropertyId == request.PropertyId.Value);
        }

        // Execute query with projection and sorting (AC-4.3.2 - sorted by date descending)
        var incomeList = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new IncomeDto(
                i.Id,
                i.PropertyId,
                i.Property.Name,
                i.Amount,
                i.Date,
                i.Source,
                i.Description,
                i.CreatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Calculate total amount from results (AC-4.3.6)
        var totalAmount = incomeList.Sum(i => i.Amount);

        return new IncomeListResult(
            Items: incomeList,
            TotalCount: incomeList.Count,
            TotalAmount: totalAmount
        );
    }
}
