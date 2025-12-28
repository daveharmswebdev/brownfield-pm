using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<(Guid? UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string email,
        string password,
        Guid accountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            AccountId = accountId,
            Role = role,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            return (user.Id, Array.Empty<string>());
        }

        return (null, result.Errors.Select(e => e.Description));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Encode the token with user ID for verification endpoint
        // Format: userId:token (both URL encoded)
        var combinedToken = $"{userId}:{HttpUtility.UrlEncode(token)}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(combinedToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Decode the token
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':', 2);

            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
            {
                return (false, "Invalid verification link");
            }

            var actualToken = HttpUtility.UrlDecode(parts[1]);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                return (false, "Invalid verification link");
            }

            if (user.EmailConfirmed)
            {
                return (false, "This verification link has already been used");
            }

            var result = await _userManager.ConfirmEmailAsync(user, actualToken);

            if (result.Succeeded)
            {
                return (true, null);
            }

            // Check for token expired error
            var isExpired = result.Errors.Any(e =>
                e.Code == "InvalidToken" ||
                e.Description.Contains("expired", StringComparison.OrdinalIgnoreCase));

            if (isExpired)
            {
                return (false, "Verification link has expired. Please request a new one.");
            }

            return (false, "Invalid verification link");
        }
        catch (FormatException)
        {
            return (false, "Invalid verification link");
        }
    }

    public async Task<(bool Success, Guid? UserId, Guid? AccountId, string? Role, string? ErrorMessage)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Find user by email (ignore query filters to search all users)
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

        // Generic error message for security (prevents user enumeration) per AC4.3
        const string invalidCredentialsError = "Invalid email or password";

        if (user == null)
        {
            return (false, null, null, null, invalidCredentialsError);
        }

        // Check if email is verified per AC4.4
        if (!user.EmailConfirmed)
        {
            return (false, null, null, null, "Please verify your email before logging in");
        }

        // Validate password
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return (false, null, null, null, invalidCredentialsError);
        }

        return (true, user.Id, user.AccountId, user.Role, null);
    }

    public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return user == Guid.Empty ? null : user;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Encode the token with user ID for reset endpoint (same pattern as email verification)
        // Format: userId:token (both URL encoded)
        var combinedToken = $"{userId}:{HttpUtility.UrlEncode(token)}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(combinedToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        // Generic error message for all failure cases (AC6.5)
        const string invalidTokenError = "This reset link is invalid or expired";

        try
        {
            // Decode the token
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':', 2);

            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
            {
                return (false, invalidTokenError);
            }

            var actualToken = HttpUtility.UrlDecode(parts[1]);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                return (false, invalidTokenError);
            }

            // Reset the password using Identity
            var result = await _userManager.ResetPasswordAsync(user, actualToken, newPassword);

            if (result.Succeeded)
            {
                return (true, null);
            }

            // Check for specific error types but return generic message
            var hasPasswordError = result.Errors.Any(e =>
                e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase));

            if (hasPasswordError)
            {
                // Return password validation errors (not security sensitive)
                var passwordErrors = result.Errors
                    .Where(e => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Description);
                return (false, string.Join(". ", passwordErrors));
            }

            // Token invalid or expired - return generic message (AC6.5)
            return (false, invalidTokenError);
        }
        catch (FormatException)
        {
            return (false, invalidTokenError);
        }
    }
}
