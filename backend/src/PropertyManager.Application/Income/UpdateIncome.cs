using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Income;

/// <summary>
/// Command for updating an existing income entry (AC-4.2.2, AC-4.2.3, AC-4.2.4).
/// PropertyId is NOT editable - delete and recreate to change property.
/// </summary>
public record UpdateIncomeCommand(
    Guid Id,
    decimal Amount,
    DateOnly Date,
    string? Source,
    string? Description
) : IRequest;

/// <summary>
/// Validator for UpdateIncomeCommand (AC-4.2.4).
/// Validates Amount > 0, Date required.
/// </summary>
public class UpdateIncomeValidator : AbstractValidator<UpdateIncomeCommand>
{
    public UpdateIncomeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Income ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than $0");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required.");
    }
}

/// <summary>
/// Handler for UpdateIncomeCommand.
/// Updates an existing income entry with AccountId validation.
/// Preserves CreatedAt, CreatedByUserId, PropertyId.
/// Sets UpdatedAt to current UTC time (AC-4.2.3).
/// </summary>
public class UpdateIncomeCommandHandler : IRequestHandler<UpdateIncomeCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateIncomeCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIncomeCommand request, CancellationToken cancellationToken)
    {
        // Find income - AccountId filtering handled by global query filter
        var income = await _dbContext.Income
            .Where(i => i.Id == request.Id && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (income == null)
        {
            throw new NotFoundException(nameof(IncomeEntity), request.Id);
        }

        // Update editable fields only (AC-4.2.2)
        income.Amount = request.Amount;
        income.Date = request.Date;
        income.Source = request.Source?.Trim();
        income.Description = request.Description?.Trim();

        // Set UpdatedAt timestamp (AC-4.2.3)
        income.UpdatedAt = DateTime.UtcNow;

        // Preserve: CreatedAt, CreatedByUserId, PropertyId, AccountId (not modified)

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
