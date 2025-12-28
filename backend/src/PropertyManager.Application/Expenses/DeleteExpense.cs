using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Command for soft-deleting an expense (AC-3.3.1, AC-3.3.3).
/// Sets DeletedAt timestamp without physically removing the record.
/// </summary>
public record DeleteExpenseCommand(
    Guid Id
) : IRequest;

/// <summary>
/// Handler for DeleteExpenseCommand.
/// Performs soft delete by setting DeletedAt to current UTC time.
/// Validates expense exists and belongs to user's account.
/// </summary>
public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteExpenseCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        // Find expense - AccountId filtering handled by global query filter
        var expense = await _dbContext.Expenses
            .Where(e => e.Id == request.Id && e.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (expense == null)
        {
            throw new NotFoundException(nameof(Expense), request.Id);
        }

        // Soft delete: Set DeletedAt timestamp (AC-3.3.3)
        expense.DeletedAt = DateTime.UtcNow;

        // Preserve all other fields unchanged

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
