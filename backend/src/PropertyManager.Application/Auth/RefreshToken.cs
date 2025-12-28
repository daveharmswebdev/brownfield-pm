using MediatR;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for refreshing an access token using a refresh token (AC4.6).
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<RefreshTokenResult>;

/// <summary>
/// Result of refresh token operation.
/// </summary>
public record RefreshTokenResult(
    string AccessToken,
    int ExpiresIn,
    string? NewRefreshToken = null
);

/// <summary>
/// Handler for RefreshTokenCommand.
/// Validates refresh token and generates new access token per AC4.6.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate the refresh token
        var (isValid, userId, accountId, role) = await _jwtService.ValidateRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!isValid || userId == null || accountId == null || role == null)
        {
            _logger.LogWarning("Invalid or expired refresh token used at {Timestamp}", DateTime.UtcNow);
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Generate new access token
        var (accessToken, expiresIn) = await _jwtService.GenerateAccessTokenAsync(
            userId.Value,
            accountId.Value,
            role,
            cancellationToken);

        _logger.LogInformation(
            "Access token refreshed for user {UserId} at {Timestamp}",
            userId,
            DateTime.UtcNow);

        // Note: We're not rotating the refresh token by default for simplicity
        // If rotation is desired, uncomment the following:
        // await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        // var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync(userId.Value, accountId.Value, cancellationToken);
        // return new RefreshTokenResult(accessToken, expiresIn, newRefreshToken);

        return new RefreshTokenResult(accessToken, expiresIn);
    }
}
