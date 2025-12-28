using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;
using PropertyEntity = PropertyManager.Domain.Entities.Property;

namespace PropertyManager.Application.Income;

/// <summary>
/// Command for creating a new income entry (AC-4.1.3).
/// </summary>
public record CreateIncomeCommand(
    Guid PropertyId,
    decimal Amount,
    DateOnly Date,
    string? Source,
    string? Description
) : IRequest<Guid>;

/// <summary>
/// Handler for CreateIncomeCommand.
/// Creates a new income entry with AccountId from current user.
/// Validates property exists and belongs to user's account.
/// </summary>
public class CreateIncomeCommandHandler : IRequestHandler<CreateIncomeCommand, Guid>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateIncomeCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateIncomeCommand request, CancellationToken cancellationToken)
    {
        // Validate property exists and belongs to user's account (global query filter handles account isolation)
        var propertyExists = await _dbContext.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException(nameof(PropertyEntity), request.PropertyId);
        }

        var income = new IncomeEntity
        {
            AccountId = _currentUser.AccountId,
            PropertyId = request.PropertyId,
            Amount = request.Amount,
            Date = request.Date,
            Source = request.Source?.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = _currentUser.UserId
        };

        _dbContext.Income.Add(income);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return income.Id;
    }
}
