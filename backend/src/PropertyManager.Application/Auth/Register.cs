using FluentValidation;
using MediatR;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for user registration.
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string AccountName
) : IRequest<RegisterResult>;

/// <summary>
/// Result of registration.
/// </summary>
public record RegisterResult(
    Guid UserId,
    bool RequiresEmailVerification = true
);

/// <summary>
/// Validator for RegisterCommand.
/// Validates password requirements per AC3.2.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(255).WithMessage("Account name must be 255 characters or less");
    }
}

/// <summary>
/// Handler for RegisterCommand.
/// Creates Account, User via Identity, generates email verification token.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IAppDbContext _dbContext;
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        IAppDbContext dbContext,
        IIdentityService identityService,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
        _emailService = emailService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists (AC3.3)
        if (await _identityService.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("Email", "An account with this email already exists")
            });
        }

        // Create Account entity
        var account = new Account
        {
            Name = request.AccountName
        };
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create User via Identity with "Owner" role
        var (userId, errors) = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            account.Id,
            "Owner",
            cancellationToken);

        if (userId is null)
        {
            // Rollback account creation
            _dbContext.Accounts.Remove(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure("Password", e)));
        }

        // Generate email verification token and send email (AC3.4)
        var token = await _identityService.GenerateEmailVerificationTokenAsync(userId.Value, cancellationToken);
        await _emailService.SendVerificationEmailAsync(request.Email, token, cancellationToken);

        return new RegisterResult(userId.Value);
    }
}
