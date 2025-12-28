using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for user login.
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

/// <summary>
/// Result of login containing JWT access token and refresh token.
/// </summary>
public record LoginResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    Guid UserId,
    Guid AccountId,
    string Role
);

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

/// <summary>
/// Handler for LoginCommand.
/// Validates credentials, generates JWT and refresh token per AC4.1, AC4.2, AC4.3, AC4.4.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IAppDbContext dbContext,
        ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Validate credentials (checks email exists, password correct, email verified)
        var (success, userId, accountId, role, errorMessage) = await _identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!success)
        {
            // Log failed login attempt for security monitoring (AC4.3)
            _logger.LogWarning(
                "Failed login attempt for email {Email} at {Timestamp}",
                request.Email,
                DateTime.UtcNow);

            throw new UnauthorizedAccessException(errorMessage ?? "Invalid email or password");
        }

        // Generate JWT access token (AC4.2)
        var (accessToken, expiresIn) = await _jwtService.GenerateAccessTokenAsync(
            userId!.Value,
            accountId!.Value,
            role!,
            cancellationToken);

        // Generate and store refresh token (AC4.6)
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(
            userId.Value,
            accountId.Value,
            cancellationToken);

        _logger.LogInformation(
            "User {UserId} logged in successfully at {Timestamp}",
            userId,
            DateTime.UtcNow);

        return new LoginResult(
            accessToken,
            expiresIn,
            refreshToken,
            userId.Value,
            accountId.Value,
            role!);
    }
}
