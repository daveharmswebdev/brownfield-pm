using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Properties;

/// <summary>
/// Command for updating an existing property (AC-2.4.5).
/// </summary>
public record UpdatePropertyCommand(
    Guid Id,
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode
) : IRequest;

/// <summary>
/// Validator for UpdatePropertyCommand (AC-2.4.4, AC-2.4.5).
/// </summary>
public class UpdatePropertyCommandValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Property ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must be 255 characters or less");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required")
            .MaximumLength(255).WithMessage("Street must be 255 characters or less");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City must be 100 characters or less");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .Length(2).WithMessage("State must be exactly 2 characters");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP Code is required")
            .Matches(@"^\d{5}$").WithMessage("ZIP Code must be exactly 5 digits");
    }
}

/// <summary>
/// Handler for UpdatePropertyCommand.
/// Updates an existing property with AccountId validation (AC-2.4.2, AC-2.4.5).
/// </summary>
public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdatePropertyCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties
            .Where(p => p.Id == request.Id && p.AccountId == _currentUser.AccountId && p.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            throw new NotFoundException("Property", request.Id);
        }

        property.Name = request.Name;
        property.Street = request.Street;
        property.City = request.City;
        property.State = request.State.ToUpperInvariant();
        property.ZipCode = request.ZipCode;
        property.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
