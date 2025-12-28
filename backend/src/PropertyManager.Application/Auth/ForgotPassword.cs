using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for requesting a password reset (AC6.1).
/// Always returns success to prevent email enumeration.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResult>;

/// <summary>
/// Result of forgot password request.
/// Always indicates success to prevent email enumeration.
/// </summary>
public record ForgotPasswordResult(bool Success = true);

/// <summary>
/// Validator for ForgotPasswordCommand.
/// Only validates email format - existence check is silent.
/// </summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

/// <summary>
/// Handler for ForgotPasswordCommand.
/// Generates reset token and sends email if user exists,
/// but always returns success to prevent email enumeration (AC6.1).
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Log password reset request for security monitoring (AC6.1)
        _logger.LogInformation("Password reset requested for email {Email}", request.Email);

        // Look up user by email (case-insensitive)
        var userId = await _identityService.GetUserIdByEmailAsync(request.Email, cancellationToken);

        if (userId.HasValue)
        {
            // User exists - generate token and send email
            var token = await _identityService.GeneratePasswordResetTokenAsync(userId.Value, cancellationToken);
            await _emailService.SendPasswordResetEmailAsync(request.Email, token, cancellationToken);

            _logger.LogInformation("Password reset email sent for user {UserId}", userId.Value);
        }
        else
        {
            // User doesn't exist - log but don't reveal this to caller (AC6.1)
            _logger.LogWarning("Password reset requested for non-existent email {Email}", request.Email);
        }

        // Always return success to prevent email enumeration (AC6.1)
        return new ForgotPasswordResult(true);
    }
}
