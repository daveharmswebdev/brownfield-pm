using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Command for updating an existing expense (AC-3.2.1, AC-3.2.3).
/// PropertyId is NOT editable - delete and recreate to change property.
/// </summary>
public record UpdateExpenseCommand(
    Guid Id,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string? Description
) : IRequest;

/// <summary>
/// Handler for UpdateExpenseCommand.
/// Updates an existing expense with AccountId validation.
/// Preserves CreatedAt, CreatedByUserId, PropertyId.
/// Sets UpdatedAt to current UTC time (AC-3.2.3).
/// </summary>
public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateExpenseCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        // Find expense - AccountId filtering handled by global query filter
        var expense = await _dbContext.Expenses
            .Where(e => e.Id == request.Id && e.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException(nameof(Expense), request.Id);
        }

        // Validate category exists
        var categoryExists = await _dbContext.ExpenseCategories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException(nameof(ExpenseCategory), request.CategoryId);
        }

        // Update editable fields only (AC-3.2.1)
        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.CategoryId = request.CategoryId;
        expense.Description = request.Description?.Trim();

        // Set UpdatedAt timestamp (AC-3.2.3)
        expense.UpdatedAt = DateTime.UtcNow;

        // Preserve: CreatedAt, CreatedByUserId, PropertyId, AccountId (not modified)

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
