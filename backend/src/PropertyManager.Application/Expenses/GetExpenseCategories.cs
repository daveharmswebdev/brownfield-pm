using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query to get all expense categories (AC-3.1.4).
/// </summary>
public record GetExpenseCategoriesQuery : IRequest<List<ExpenseCategoryDto>>;

/// <summary>
/// Handler for GetExpenseCategoriesQuery.
/// Returns all 15 IRS Schedule E categories ordered by SortOrder.
/// </summary>
public class GetExpenseCategoriesQueryHandler : IRequestHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetExpenseCategoriesQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ExpenseCategoryDto>> Handle(GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.ExpenseCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new ExpenseCategoryDto(
                c.Id,
                c.Name,
                c.ScheduleELine,
                c.SortOrder
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
