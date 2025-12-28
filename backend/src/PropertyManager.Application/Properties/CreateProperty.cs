using FluentValidation;
using MediatR;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Properties;

/// <summary>
/// Command for creating a new property.
/// </summary>
public record CreatePropertyCommand(
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode
) : IRequest<Guid>;

/// <summary>
/// Validator for CreatePropertyCommand (AC-2.1.5).
/// </summary>
public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
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
/// Handler for CreatePropertyCommand.
/// Creates a new property with AccountId from current user (AC-2.1.4).
/// </summary>
public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePropertyCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = new Property
        {
            AccountId = _currentUser.AccountId,
            Name = request.Name,
            Street = request.Street,
            City = request.City,
            State = request.State.ToUpperInvariant(),
            ZipCode = request.ZipCode
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return property.Id;
    }
}
