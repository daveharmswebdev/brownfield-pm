using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Infrastructure.Identity;

/// <summary>
/// Implementation of IJwtService for JWT token generation and refresh token management.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _dbContext;

    public JwtService(
        IOptions<JwtSettings> settings,
        AppDbContext dbContext)
    {
        _settings = settings.Value;
        _dbContext = dbContext;
    }

    public Task<(string AccessToken, int ExpiresIn)> GenerateAccessTokenAsync(
        Guid userId,
        Guid accountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var expiresIn = _settings.AccessTokenExpiryMinutes * 60; // Convert to seconds
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        // Create claims per AC4.2
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("userId", userId.ToString()),
            new("accountId", accountId.ToString()),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult((tokenString, expiresIn));
    }

    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure random token
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var refreshToken = Convert.ToBase64String(randomBytes);

        // Hash the token for secure storage
        var tokenHash = HashToken(refreshToken);

        // Create RefreshToken entity
        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            AccountId = accountId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays),
            DeviceInfo = null // Can be populated from request headers if needed
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<(bool IsValid, Guid? UserId, Guid? AccountId, string? Role)> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        // Find the refresh token in database (ignore query filters for this lookup)
        var storedToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken == null || !storedToken.IsValid)
        {
            return (false, null, null, null);
        }

        // Get user info
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, cancellationToken);

        if (user == null)
        {
            return (false, null, null, null);
        }

        return (true, storedToken.UserId, storedToken.AccountId, user.Role);
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken != null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hash the refresh token using SHA256 for secure storage.
    /// </summary>
    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
