using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query to get all expenses across all properties with filtering and pagination (AC-3.4.1, AC-3.4.3, AC-3.4.4, AC-3.4.5, AC-3.4.8).
/// </summary>
public record GetAllExpensesQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    List<Guid>? CategoryIds,
    string? Search,
    int? Year,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<ExpenseListItemDto>>;

/// <summary>
/// Paginated result response for list queries (AC-3.4.8).
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// DTO for expense list item display (AC-3.4.2).
/// Includes PropertyName for cross-property list view.
/// </summary>
public record ExpenseListItemDto(
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

/// <summary>
/// Handler for GetAllExpensesQuery.
/// Returns paginated expenses across all properties with optional filtering by:
/// - Date range (DateFrom, DateTo)
/// - Tax year
/// - Categories (multi-select)
/// - Description search (case-insensitive, partial match)
/// Results sorted by Date descending (newest first).
/// </summary>
public class GetAllExpensesHandler : IRequestHandler<GetAllExpensesQuery, PagedResult<ExpenseListItemDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetAllExpensesHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ExpenseListItemDto>> Handle(GetAllExpensesQuery request, CancellationToken cancellationToken)
    {
        // Start with base query - global query filter handles AccountId isolation
        var query = _dbContext.Expenses.AsQueryable();

        // Apply year filter (AC-3.4.1 - respects global tax year selector)
        if (request.Year.HasValue)
        {
            var yearStart = new DateOnly(request.Year.Value, 1, 1);
            var yearEnd = new DateOnly(request.Year.Value, 12, 31);
            query = query.Where(e => e.Date >= yearStart && e.Date <= yearEnd);
        }

        // Apply date range filter (AC-3.4.3)
        if (request.DateFrom.HasValue)
        {
            query = query.Where(e => e.Date >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(e => e.Date <= request.DateTo.Value);
        }

        // Apply category filter - additive (any selected category matches) (AC-3.4.4)
        if (request.CategoryIds != null && request.CategoryIds.Count > 0)
        {
            query = query.Where(e => request.CategoryIds.Contains(e.CategoryId));
        }

        // Apply search filter - case-insensitive, partial match (AC-3.4.5)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(e => e.Description != null &&
                                     e.Description.ToLower().Contains(searchTerm));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Calculate pagination
        var pageSize = Math.Clamp(request.PageSize, 1, 100); // Max 100 per page
        var page = Math.Max(1, request.Page);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (page - 1) * pageSize;

        // Execute query with sorting and pagination (AC-3.4.1 - sorted by date descending)
        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(e => new ExpenseListItemDto(
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

        return new PagedResult<ExpenseListItemDto>(
            Items: expenses,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages
        );
    }
}
