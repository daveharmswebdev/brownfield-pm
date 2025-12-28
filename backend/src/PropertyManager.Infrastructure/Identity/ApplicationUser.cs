using Microsoft.AspNetCore.Identity;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user with custom properties for multi-tenancy.
/// Links to Account for tenant isolation.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Foreign key to the Account (tenant boundary).
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// User role within the account: "Owner" or "Contributor".
    /// </summary>
    public string Role { get; set; } = "Owner";

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public Account Account { get; set; } = null!;
}
