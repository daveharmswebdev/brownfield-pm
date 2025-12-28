using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query to get expenses for a specific property (AC-3.1.7).
/// </summary>
public record GetExpensesByPropertyQuery(
    Guid PropertyId,
    int? Year = null
) : IRequest<ExpenseListDto>;

/// <summary>
/// Response DTO for expenses by property.
/// </summary>
public record ExpenseListDto(
    List<ExpenseDto> Items,
    int TotalCount,
    decimal YtdTotal
);

/// <summary>
/// Handler for GetExpensesByPropertyQuery.
/// Returns expenses for a property, ordered by Date descending (newest first).
/// Optionally filtered by year.
/// </summary>
public class GetExpensesByPropertyQueryHandler : IRequestHandler<GetExpensesByPropertyQuery, ExpenseListDto>
{
    private readonly IAppDbContext _dbContext;

    public GetExpensesByPropertyQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExpenseListDto> Handle(GetExpensesByPropertyQuery request, CancellationToken cancellationToken)
    {
        // Validate property exists (global query filter handles account isolation)
        var propertyExists = await _dbContext.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException(nameof(Property), request.PropertyId);
        }

        var query = _dbContext.Expenses
            .Where(e => e.PropertyId == request.PropertyId);

        // Apply year filter if specified
        if (request.Year.HasValue)
        {
            var startDate = new DateOnly(request.Year.Value, 1, 1);
            var endDate = new DateOnly(request.Year.Value, 12, 31);
            query = query.Where(e => e.Date >= startDate && e.Date <= endDate);
        }

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseDto(
                e.Id,
                e.PropertyId,
                e.Property.Name,
                e.CategoryId,
                e.Category.Name,
                e.Category.ScheduleELine,
                e.Amount,
                e.Date,
                e.Description,
                e.ReceiptId,
                e.CreatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ytdTotal = expenses.Sum(e => e.Amount);

        return new ExpenseListDto(
            Items: expenses,
            TotalCount: expenses.Count,
            YtdTotal: ytdTotal
        );
    }
}
