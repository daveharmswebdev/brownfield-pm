namespace PropertyManager.Infrastructure.Identity;

/// <summary>
/// Configuration settings for JWT token generation.
/// Bound from appsettings.json "Jwt" section.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing JWT tokens.
    /// Must be at least 256 bits (32 characters) for HS256.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer (iss claim).
    /// </summary>
    public string Issuer { get; set; } = "PropertyManager";

    /// <summary>
    /// JWT token audience (aud claim).
    /// </summary>
    public string Audience { get; set; } = "PropertyManager";

    /// <summary>
    /// Access token expiry in minutes (default: 60 per AC4.2).
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiry in days (default: 7 per AC4.6).
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
