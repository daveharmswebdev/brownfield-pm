using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Query for getting a single expense by ID (AC-3.2.1, AC-3.2.2).
/// </summary>
public record GetExpenseQuery(Guid Id) : IRequest<ExpenseDto>;

/// <summary>
/// Handler for GetExpenseQuery.
/// Returns expense with category details.
/// AccountId filtering handled by global query filter.
/// </summary>
public class GetExpenseQueryHandler : IRequestHandler<GetExpenseQuery, ExpenseDto>
{
    private readonly IAppDbContext _dbContext;

    public GetExpenseQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExpenseDto> Handle(GetExpenseQuery request, CancellationToken cancellationToken)
    {
        var expense = await _dbContext.Expenses
            .Where(e => e.Id == request.Id && e.DeletedAt == null)
            .Include(e => e.Property)
            .Include(e => e.Category)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException(nameof(Expense), request.Id);
        }

        return expense;
    }
}
