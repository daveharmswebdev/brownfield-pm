using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for user registration via invitation token.
/// Invitation token replaces public registration.
/// </summary>
public record RegisterCommand(
    string Password,
    string InvitationToken
) : IRequest<RegisterResult>;

/// <summary>
/// Result of registration.
/// </summary>
public record RegisterResult(
    Guid UserId,
    bool RequiresEmailVerification = false
);

/// <summary>
/// Validator for RegisterCommand.
/// Validates password requirements per AC3.2.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.InvitationToken)
            .NotEmpty().WithMessage("Invitation token is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?]").WithMessage("Password must contain at least one special character");
    }
}

/// <summary>
/// Handler for RegisterCommand.
/// Validates invitation token, creates Account, User via Identity.
/// Invited users have email already verified.
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
        // Hash the token for lookup
        var tokenHash = HashToken(request.InvitationToken);

        // Find the invitation by token hash (ignoring tenant filter since user isn't authenticated)
        var invitation = await _dbContext.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

        // Validate invitation exists, is not expired, and not already used
        if (invitation == null || invitation.IsExpired || invitation.IsAccepted)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("InvitationToken", "This invitation link is invalid or expired")
            });
        }

        var email = invitation.Email;

        // Check if email already exists (shouldn't happen if invitation system is working correctly)
        if (await _identityService.EmailExistsAsync(email, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("Email", "An account with this email already exists")
            });
        }

        // Derive account name from email (prefix before @)
        var accountName = DeriveAccountNameFromEmail(email);

        // Create Account entity
        var account = new Account
        {
            Name = accountName
        };
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create User via Identity with "Owner" role and email already confirmed
        var (userId, errors) = await _identityService.CreateUserAsync(
            email,
            request.Password,
            account.Id,
            "Owner",
            cancellationToken,
            emailConfirmed: true); // Invitation = trusted email

        if (userId is null)
        {
            // Rollback account creation
            _dbContext.Accounts.Remove(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure("Password", e)));
        }

        // Mark invitation as accepted
        invitation.AcceptedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // No email verification needed for invited users
        return new RegisterResult(userId.Value, RequiresEmailVerification: false);
    }

    private static string DeriveAccountNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex > 0)
        {
            return email[..atIndex];
        }
        return email;
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
