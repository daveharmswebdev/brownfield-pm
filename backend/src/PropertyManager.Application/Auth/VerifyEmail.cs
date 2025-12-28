using FluentValidation;
using MediatR;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for email verification.
/// </summary>
public record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResult>;

/// <summary>
/// Result of email verification.
/// </summary>
public record VerifyEmailResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Validator for VerifyEmailCommand.
/// </summary>
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required");
    }
}

/// <summary>
/// Handler for VerifyEmailCommand.
/// Validates the email verification token and marks user as verified.
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IIdentityService _identityService;

    public VerifyEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await _identityService.VerifyEmailAsync(request.Token, cancellationToken);

        return new VerifyEmailResult(success, errorMessage);
    }
}
