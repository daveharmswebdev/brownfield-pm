using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Income;

/// <summary>
/// Command for soft-deleting an income entry (AC-4.2.5, AC-4.2.6).
/// Sets DeletedAt timestamp without physically removing the record.
/// </summary>
public record DeleteIncomeCommand(
    Guid Id
) : IRequest;

/// <summary>
/// Handler for DeleteIncomeCommand.
/// Performs soft delete by setting DeletedAt to current UTC time.
/// Validates income exists and belongs to user's account.
/// </summary>
public class DeleteIncomeCommandHandler : IRequestHandler<DeleteIncomeCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteIncomeCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteIncomeCommand request, CancellationToken cancellationToken)
    {
        // Find income - AccountId filtering handled by global query filter
        var income = await _dbContext.Income
            .Where(i => i.Id == request.Id && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (income == null)
        {
            throw new NotFoundException(nameof(IncomeEntity), request.Id);
        }

        // Soft delete: Set DeletedAt timestamp (AC-4.2.6)
        income.DeletedAt = DateTime.UtcNow;

        // Preserve all other fields unchanged

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
