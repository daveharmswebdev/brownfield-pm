using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;

namespace PropertyManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the database with initial data for development and E2E testing.
/// Creates a seed owner user that can send invitations.
/// </summary>
public class DatabaseSeeder
{
    // Well-known credentials for E2E tests
    public const string SeedOwnerEmail = "e2e-owner@test.local";
    public const string SeedOwnerPassword = "E2eTest@123456";
    public const string SeedAccountName = "E2E Test Account";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial data if it doesn't already exist.
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedOwnerUserAsync();
    }

    private async Task SeedOwnerUserAsync()
    {
        // Check if seed owner already exists
        var existingUser = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == SeedOwnerEmail);

        if (existingUser != null)
        {
            _logger.LogInformation("Seed owner user already exists: {Email}", SeedOwnerEmail);
            return;
        }

        _logger.LogInformation("Creating seed owner user: {Email}", SeedOwnerEmail);

        // Create account for the seed owner
        var account = new Account { Name = SeedAccountName };
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        // Create the owner user
        var user = new ApplicationUser
        {
            UserName = SeedOwnerEmail,
            Email = SeedOwnerEmail,
            EmailConfirmed = true, // Pre-confirmed for E2E tests
            AccountId = account.Id,
            Role = "Owner"
        };

        var result = await _userManager.CreateAsync(user, SeedOwnerPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Seed owner user created successfully: {Email}", SeedOwnerEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create seed owner user: {Errors}", errors);
            throw new InvalidOperationException($"Failed to create seed owner user: {errors}");
        }
    }
}
