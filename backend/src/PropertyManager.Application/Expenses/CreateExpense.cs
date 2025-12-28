using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Command for creating a new expense (AC-3.1.1, AC-3.1.6).
/// </summary>
public record CreateExpenseCommand(
    Guid PropertyId,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string? Description
) : IRequest<Guid>;

/// <summary>
/// Handler for CreateExpenseCommand.
/// Creates a new expense with AccountId from current user.
/// Validates property and category exist and belong to user's account.
/// </summary>
public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateExpenseCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        // Validate property exists and belongs to user's account (global query filter handles account isolation)
        var propertyExists = await _dbContext.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException(nameof(Property), request.PropertyId);
        }

        // Validate category exists (ExpenseCategories are global, no account filter)
        var categoryExists = await _dbContext.ExpenseCategories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException(nameof(ExpenseCategory), request.CategoryId);
        }

        var expense = new Expense
        {
            AccountId = _currentUser.AccountId,
            PropertyId = request.PropertyId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description?.Trim(),
            CreatedByUserId = _currentUser.UserId
        };

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expense.Id;
    }
}
