using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Application.Auth;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Authentication endpoints for registration, login, and token refresh.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<ForgotPasswordCommand> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordCommand> _resetPasswordValidator;
    private readonly IValidator<SendInvitationCommand> _sendInvitationValidator;
    private readonly ILogger<AuthController> _logger;

    // Cookie name for refresh token
    private const string RefreshTokenCookieName = "refreshToken";

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator,
        IValidator<ForgotPasswordCommand> forgotPasswordValidator,
        IValidator<ResetPasswordCommand> resetPasswordValidator,
        IValidator<SendInvitationCommand> sendInvitationValidator,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _sendInvitationValidator = sendInvitationValidator;
        _logger = logger;
    }

/// <summary>
    /// Register a new user account using an invitation token.
    /// Creates an Account and User with "Owner" role.
    /// Invited users have email already verified.
    /// </summary>
    /// <param name="request">Registration details with invitation token</param>
    /// <returns>User ID on success</returns>
    /// <response code="201">Returns the newly created user's ID</response>
    /// <response code="400">If validation fails, token is invalid/expired, or email already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Password, request.Token);

        // Validate command
        var validationResult = await _registerValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        try
        {
            var result = await _mediator.Send(command);
            var response = new RegisterResponse(result.UserId);

            return CreatedAtAction(
                nameof(Register),
                new { userId = result.UserId },
                response);
        }
        catch (ValidationException ex)
        {
            var problemDetails = CreateValidationProblemDetails(ex);
            return BadRequest(problemDetails);
        }
    }

    /// <summary>
    /// Verify email address using the token from verification email.
    /// </summary>
    /// <param name="request">Verification token</param>
    /// <returns>204 No Content on success</returns>
    /// <response code="204">Email verified successfully</response>
    /// <response code="400">If token is invalid, expired, or already used</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(CreateProblemDetails("Invalid verification link"));
        }

        var command = new VerifyEmailCommand(request.Token);
        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return NoContent();
        }

        return BadRequest(CreateProblemDetails(result.ErrorMessage ?? "Invalid verification link"));
    }

    /// <summary>
    /// Log in with email and password.
    /// Returns JWT access token in response body and refresh token as HttpOnly cookie (AC4.1).
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Access token and expiration</returns>
    /// <response code="200">Login successful, returns JWT access token</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If credentials are invalid or email not verified</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);

        // Validate command
        var validationResult = await _loginValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        try
        {
            var result = await _mediator.Send(command);

            // Set refresh token as HttpOnly cookie (AC4.1)
            SetRefreshTokenCookie(result.RefreshToken);

            var response = new LoginResponse(result.AccessToken, result.ExpiresIn);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Log failed login attempt for security monitoring (AC4.3)
            _logger.LogWarning(
                "Failed login attempt for email {Email}: {Error}",
                request.Email,
                ex.Message);

            return Unauthorized(CreateProblemDetails(ex.Message));
        }
    }

    /// <summary>
    /// Refresh access token using the refresh token from HttpOnly cookie (AC4.6).
    /// </summary>
    /// <returns>New access token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">If refresh token is invalid or expired</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        // Read refresh token from cookie
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(CreateProblemDetails("No refresh token provided"));
        }

        try
        {
            var command = new RefreshTokenCommand(refreshToken);
            var result = await _mediator.Send(command);

            // If a new refresh token was issued (rotation), update the cookie
            if (!string.IsNullOrWhiteSpace(result.NewRefreshToken))
            {
                SetRefreshTokenCookie(result.NewRefreshToken);
            }

            var response = new RefreshResponse(result.AccessToken, result.ExpiresIn);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Clear the invalid refresh token cookie
            ClearRefreshTokenCookie();

            return Unauthorized(CreateProblemDetails(ex.Message));
        }
    }

    /// <summary>
    /// Log out the current user by invalidating their refresh token (AC5.1, AC5.2).
    /// Clears the refresh token cookie.
    /// </summary>
    /// <returns>204 No Content on success</returns>
    /// <response code="204">Logout successful, refresh token invalidated</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        // Read refresh token from cookie
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        // Send logout command to invalidate the token in database
        var command = new LogoutCommand(refreshToken);
        await _mediator.Send(command);

        // Clear the refresh token cookie (AC5.1)
        ClearRefreshTokenCookie();

        // Log successful logout for security monitoring
        _logger.LogInformation(
            "User logged out successfully at {Timestamp}",
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Request a password reset email (AC6.1).
    /// Always returns 204 No Content to prevent email enumeration.
    /// </summary>
    /// <param name="request">Email address to send reset link</param>
    /// <returns>204 No Content (always)</returns>
    /// <response code="204">Request processed (email sent if account exists)</response>
    /// <response code="400">If validation fails</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);

        // Validate command
        var validationResult = await _forgotPasswordValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        // Send command - always succeeds to prevent email enumeration (AC6.1)
        await _mediator.Send(command);

        // Log password reset request for security monitoring
        _logger.LogInformation(
            "Password reset requested for email {Email} at {Timestamp}",
            request.Email,
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Reset password using token from email (AC6.3).
    /// Validates token and updates password.
    /// Invalidates all existing sessions (AC6.4).
    /// </summary>
    /// <param name="request">Reset token and new password</param>
    /// <returns>204 No Content on success</returns>
    /// <response code="204">Password reset successful</response>
    /// <response code="400">If token is invalid/expired or password validation fails</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);

        // Validate command
        var validationResult = await _resetPasswordValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation("Password reset completed successfully at {Timestamp}", DateTime.UtcNow);
            return NoContent();
        }

        // Return error with generic message (AC6.5)
        return BadRequest(CreateProblemDetails(result.ErrorMessage ?? "This reset link is invalid or expired"));
    }

    /// <summary>
    /// Send an invitation email to a new user.
    /// Only accessible by authenticated users with "Owner" role.
    /// Creates an invitation with 24-hour expiry.
    /// </summary>
    /// <param name="request">Email address to invite</param>
    /// <returns>Success status</returns>
    /// <response code="200">Invitation sent successfully</response>
    /// <response code="400">If validation fails or invitation already exists</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user is not an Owner</response>
    [HttpPost("invite")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Owner")]
    [ProducesResponseType(typeof(SendInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendInvitation([FromBody] SendInvitationRequest request)
    {
        var command = new SendInvitationCommand(request.Email);

        // Validate command
        var validationResult = await _sendInvitationValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        try
        {
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(CreateProblemDetails(result.Error ?? "Failed to send invitation"));
            }

            _logger.LogInformation("Invitation sent to {Email} at {Timestamp}", request.Email, DateTime.UtcNow);
            return Ok(new SendInvitationResponse(true, "Invitation sent successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(ex.Message));
        }
    }

    /// <summary>
    /// Sets the refresh token as an HttpOnly cookie with security flags (AC4.1).
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,  // Prevents XSS attacks
            Secure = true,    // Only sent over HTTPS
            SameSite = SameSiteMode.Strict,  // Prevents CSRF attacks
            Expires = DateTime.UtcNow.AddDays(7),  // 7 day expiry per AC4.6
            Path = "/api/v1/auth"  // Restrict to auth endpoints
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    /// <summary>
    /// Clears the refresh token cookie.
    /// </summary>
    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }

    private ProblemDetails CreateProblemDetails(string detail)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }

    private ValidationProblemDetails CreateValidationProblemDetails(FluentValidation.Results.ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }

    private ValidationProblemDetails CreateValidationProblemDetails(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }
}

/// <summary>
/// Request model for user registration via invitation.
/// </summary>
public record RegisterRequest(
    string Password,
    string Token
);

/// <summary>
/// Response model for successful registration.
/// </summary>
public record RegisterResponse(
    Guid UserId
);

/// <summary>
/// Request model for email verification.
/// </summary>
public record VerifyEmailRequest(
    string Token
);

/// <summary>
/// Request model for user login.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Response model for successful login (AC4.1).
/// </summary>
public record LoginResponse(
    string AccessToken,
    int ExpiresIn
);

/// <summary>
/// Response model for successful token refresh.
/// </summary>
public record RefreshResponse(
    string AccessToken,
    int ExpiresIn
);

/// <summary>
/// Request model for password reset request (AC6.1).
/// </summary>
public record ForgotPasswordRequest(
    string Email
);

/// <summary>
/// Request model for password reset (AC6.3).
/// </summary>
public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

/// <summary>
/// Request model for sending an invitation.
/// </summary>
public record SendInvitationRequest(
    string Email
);

/// <summary>
/// Response model for invitation.
/// </summary>
public record SendInvitationResponse(
    bool Success,
    string Message
);
