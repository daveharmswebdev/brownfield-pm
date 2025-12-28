using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for resetting password with token (AC6.3).
/// </summary>
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResult>;

/// <summary>
/// Result of password reset.
/// </summary>
public record ResetPasswordResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Validator for ResetPasswordCommand.
/// Validates password strength requirements (same as registration per AC6.3).
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?]").WithMessage("Password must contain at least one special character");
    }
}

/// <summary>
/// Handler for ResetPasswordCommand.
/// Validates token, resets password, and invalidates all sessions (AC6.3, AC6.4, AC6.5).
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing password reset request");

        // Reset password via Identity (validates token internally)
        var (success, errorMessage) = await _identityService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Password reset failed: {Error}", errorMessage);
            return new ResetPasswordResult(false, errorMessage);
        }

        // Extract userId from token to revoke sessions (AC6.4)
        var userId = ExtractUserIdFromToken(request.Token);
        if (userId.HasValue)
        {
            // Revoke all refresh tokens for this user (invalidate all sessions) - AC6.4
            await _jwtService.RevokeAllUserRefreshTokensAsync(userId.Value, cancellationToken);
            _logger.LogInformation(
                "Password reset completed for user {UserId}, all sessions invalidated",
                userId.Value);
        }
        else
        {
            _logger.LogWarning("Could not extract userId from token for session invalidation");
        }

        return new ResetPasswordResult(true);
    }

    /// <summary>
    /// Extracts userId from the encoded token.
    /// Token format: Base64(userId:actualToken)
    /// </summary>
    private static Guid? ExtractUserIdFromToken(string token)
    {
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':', 2);

            if (parts.Length == 2 && Guid.TryParse(parts[0], out var userId))
            {
                return userId;
            }
        }
        catch (FormatException)
        {
            // Invalid token format
        }

        return null;
    }
}
