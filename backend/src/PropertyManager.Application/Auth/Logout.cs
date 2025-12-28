using MediatR;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for user logout.
/// Takes the refresh token to invalidate (read from HttpOnly cookie by controller).
/// </summary>
public record LogoutCommand(
    string? RefreshToken
) : IRequest<LogoutResult>;

/// <summary>
/// Result of logout operation.
/// </summary>
public record LogoutResult(
    bool Success
);

/// <summary>
/// Handler for LogoutCommand.
/// Invalidates the refresh token to terminate the session (AC5.1, AC5.2).
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResult>
{
    private readonly IJwtService _jwtService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IJwtService jwtService,
        ICurrentUser currentUser,
        ILogger<LogoutCommandHandler> logger)
    {
        _jwtService = jwtService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Logout should be idempotent - calling it multiple times is safe (AC5.2)
        // Even if no token is provided or token not found, return success

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            // Revoke the specific refresh token (not all user tokens - AC5.2)
            // This only invalidates the current device's session
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

            _logger.LogInformation(
                "User {UserId} logged out successfully at {Timestamp}",
                _currentUser.UserId,
                DateTime.UtcNow);
        }
        else
        {
            _logger.LogInformation(
                "Logout called without refresh token for user {UserId} at {Timestamp}",
                _currentUser.UserId,
                DateTime.UtcNow);
        }

        return new LogoutResult(true);
    }
}
