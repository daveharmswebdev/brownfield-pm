using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;

namespace PropertyManager.Infrastructure.Tests;

/// <summary>
/// Integration tests for database schema creation and migrations (AC: 2.1).
/// </summary>
[Collection("Database")]
public class DatabaseSchemaTests
{
    private readonly DatabaseFixture _fixture;

    public DatabaseSchemaTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_CreatesAllTables()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        // Act & Assert - Verify all 7 tables exist by querying them
        var accounts = await context.Accounts.CountAsync();
        var users = await context.Users.CountAsync();
        var properties = await context.Properties.IgnoreQueryFilters().CountAsync();
        var expenses = await context.Expenses.IgnoreQueryFilters().CountAsync();
        var income = await context.Income.IgnoreQueryFilters().CountAsync();
        var receipts = await context.Receipts.IgnoreQueryFilters().CountAsync();
        var categories = await context.ExpenseCategories.CountAsync();

        // Tables should be queryable (even if empty)
        accounts.Should().BeGreaterThanOrEqualTo(0);
        users.Should().BeGreaterThanOrEqualTo(0);
        properties.Should().BeGreaterThanOrEqualTo(0);
        expenses.Should().BeGreaterThanOrEqualTo(0);
        income.Should().BeGreaterThanOrEqualTo(0);
        receipts.Should().BeGreaterThanOrEqualTo(0);
        categories.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PrimaryKeys_UseGuidsWithDefaultGeneration()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var account = new Account { Name = "Test Account" };

        // Act - Insert without specifying ID
        account.Id = Guid.Empty; // EF should generate a UUID
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Assert
        account.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task AuditFields_AreAutoPopulatedOnInsert()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var account = new Account { Name = "Audit Test Account" };

        // Act
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Assert
        account.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AuditFields_UpdatedAtChangesOnModification()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var account = new Account { Name = "Update Test Account" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            AccountId = account.Id,
            Email = $"update-test-{Guid.NewGuid()}@example.com",
            UserName = $"update-test-{Guid.NewGuid()}@example.com",
            NormalizedEmail = $"UPDATE-TEST-{Guid.NewGuid()}@EXAMPLE.COM",
            NormalizedUserName = $"UPDATE-TEST-{Guid.NewGuid()}@EXAMPLE.COM",
            PasswordHash = "hash",
            Role = "Owner"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var originalUpdatedAt = user.UpdatedAt;

        // Wait a moment
        await Task.Delay(100);

        // Act
        user.Email = $"updated-{Guid.NewGuid()}@example.com";
        await context.SaveChangesAsync();

        // Assert
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
