using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;

namespace PropertyManager.Infrastructure.Tests;

/// <summary>
/// Integration tests for soft delete global query filters (AC: 2.3).
/// </summary>
[Collection("Database")]
public class SoftDeleteFilterTests
{
    private readonly DatabaseFixture _fixture;

    public SoftDeleteFilterTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SoftDeleteFilter_ExcludesDeletedRecords()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        var account = new Account { Name = "Soft Delete Test Account" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var property = new Property
        {
            AccountId = account.Id,
            Name = "Deletable Property",
            Street = "123 Test St",
            City = "Test City",
            State = "TX",
            ZipCode = "12345"
        };
        context.Properties.Add(property);
        await context.SaveChangesAsync();

        // Act - Soft delete the property
        property.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Create a new context to avoid caching
        await using var newContext = _fixture.CreateDbContext();

        // Assert - Property should be excluded from normal queries
        var propertiesQuery = await newContext.Properties
            .Where(p => p.AccountId == account.Id)
            .ToListAsync();

        propertiesQuery.Should().NotContain(p => p.Name == "Deletable Property");
    }

    [Fact]
    public async Task SoftDeleteFilter_IgnoreQueryFilters_IncludesDeletedRecords()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        var account = new Account { Name = "Ignore Filters Test Account" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var property = new Property
        {
            AccountId = account.Id,
            Name = "Another Deletable Property",
            Street = "456 Test Ave",
            City = "Test City",
            State = "CA",
            ZipCode = "90210"
        };
        context.Properties.Add(property);
        await context.SaveChangesAsync();

        // Soft delete
        property.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Create a new context to avoid caching
        await using var newContext = _fixture.CreateDbContext();

        // Act - Query with IgnoreQueryFilters
        var allProperties = await newContext.Properties
            .IgnoreQueryFilters()
            .Where(p => p.AccountId == account.Id)
            .ToListAsync();

        // Assert - Deleted property should be included
        allProperties.Should().Contain(p => p.Name == "Another Deletable Property");
    }

    [Fact]
    public async Task SoftDeleteFilter_WorksOnAllSoftDeletableEntities()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        var account = new Account { Name = "Multi-Entity Soft Delete Test" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var userEmail = $"multi-delete-{Guid.NewGuid()}@example.com";
        var user = new ApplicationUser
        {
            AccountId = account.Id,
            Email = userEmail,
            UserName = userEmail,
            NormalizedEmail = userEmail.ToUpperInvariant(),
            NormalizedUserName = userEmail.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = "Owner"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var property = new Property
        {
            AccountId = account.Id,
            Name = "Multi-Delete Property",
            Street = "789 Multi St",
            City = "Test City",
            State = "NY",
            ZipCode = "10001"
        };
        context.Properties.Add(property);
        await context.SaveChangesAsync();

        // Create income and expense entries
        var income = new Income
        {
            AccountId = account.Id,
            PropertyId = property.Id,
            Amount = 1000,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Source = "Rent",
            CreatedByUserId = user.Id
        };
        context.Income.Add(income);

        var category = await context.ExpenseCategories.FirstAsync();
        var expense = new Expense
        {
            AccountId = account.Id,
            PropertyId = property.Id,
            CategoryId = category.Id,
            Amount = 100,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Description = "Test expense",
            CreatedByUserId = user.Id
        };
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();

        // Act - Soft delete all
        income.DeletedAt = DateTime.UtcNow;
        expense.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Create new context
        await using var newContext = _fixture.CreateDbContext();

        // Assert - Both should be excluded
        var activeIncome = await newContext.Income.Where(i => i.AccountId == account.Id).CountAsync();
        var activeExpenses = await newContext.Expenses.Where(e => e.AccountId == account.Id).CountAsync();

        activeIncome.Should().Be(0);
        activeExpenses.Should().Be(0);

        // But should exist when ignoring filters
        var allIncome = await newContext.Income.IgnoreQueryFilters().Where(i => i.AccountId == account.Id).CountAsync();
        var allExpenses = await newContext.Expenses.IgnoreQueryFilters().Where(e => e.AccountId == account.Id).CountAsync();

        allIncome.Should().Be(1);
        allExpenses.Should().Be(1);
    }
}
