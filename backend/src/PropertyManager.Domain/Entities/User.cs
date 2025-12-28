namespace PropertyManager.Domain.Entities;

/// <summary>
/// User DTO/value object representing user data.
/// Note: For persistence, ApplicationUser (ASP.NET Core Identity) is used.
/// This entity can be used as a projection or DTO in the Application layer.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Owner"; // Owner | Contributor
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
