using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;

namespace PropertyManager.Infrastructure.Tests;

/// <summary>
/// Integration tests for AccountId tenant isolation global query filters (AC: 2.4).
/// </summary>
[Collection("Database")]
public class TenantIsolationTests
{
    private readonly DatabaseFixture _fixture;

    public TenantIsolationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TenantFilter_EnforcesIsolation_OnlyReturnsOwnData()
    {
        // Arrange - Create two accounts with properties
        await using var setupContext = _fixture.CreateDbContext();

        var account1 = new Account { Name = "Account 1" };
        var account2 = new Account { Name = "Account 2" };
        setupContext.Accounts.AddRange(account1, account2);
        await setupContext.SaveChangesAsync();

        var property1 = new Property { AccountId = account1.Id, Name = "Property for Account 1" };
        var property2 = new Property { AccountId = account2.Id, Name = "Property for Account 2" };
        setupContext.Properties.AddRange(property1, property2);
        await setupContext.SaveChangesAsync();

        // Act - Query as Account 1
        var user1 = new TestCurrentUser { AccountId = account1.Id, UserId = Guid.NewGuid(), IsAuthenticated = true };
        await using var context1 = _fixture.CreateDbContext(user1);
        var propertiesForAccount1 = await context1.Properties.ToListAsync();

        // Act - Query as Account 2
        var user2 = new TestCurrentUser { AccountId = account2.Id, UserId = Guid.NewGuid(), IsAuthenticated = true };
        await using var context2 = _fixture.CreateDbContext(user2);
        var propertiesForAccount2 = await context2.Properties.ToListAsync();

        // Assert
        propertiesForAccount1.Should().OnlyContain(p => p.AccountId == account1.Id);
        propertiesForAccount1.Should().Contain(p => p.Name == "Property for Account 1");
        propertiesForAccount1.Should().NotContain(p => p.Name == "Property for Account 2");

        propertiesForAccount2.Should().OnlyContain(p => p.AccountId == account2.Id);
        propertiesForAccount2.Should().Contain(p => p.Name == "Property for Account 2");
        propertiesForAccount2.Should().NotContain(p => p.Name == "Property for Account 1");
    }

    [Fact]
    public async Task TenantFilter_WorksForUsers()
    {
        // Arrange
        await using var setupContext = _fixture.CreateDbContext();

        var account1 = new Account { Name = "User Isolation Account 1" };
        var account2 = new Account { Name = "User Isolation Account 2" };
        setupContext.Accounts.AddRange(account1, account2);
        await setupContext.SaveChangesAsync();

        var email1 = $"user1-{Guid.NewGuid()}@account1.com";
        var email2 = $"user2-{Guid.NewGuid()}@account2.com";
        var user1 = new ApplicationUser
        {
            AccountId = account1.Id,
            Email = email1,
            UserName = email1,
            NormalizedEmail = email1.ToUpperInvariant(),
            NormalizedUserName = email1.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = "Owner"
        };
        var user2 = new ApplicationUser
        {
            AccountId = account2.Id,
            Email = email2,
            UserName = email2,
            NormalizedEmail = email2.ToUpperInvariant(),
            NormalizedUserName = email2.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = "Owner"
        };
        setupContext.Users.AddRange(user1, user2);
        await setupContext.SaveChangesAsync();

        // Act - Query as Account 1
        var currentUser1 = new TestCurrentUser { AccountId = account1.Id, UserId = user1.Id, IsAuthenticated = true };
        await using var context1 = _fixture.CreateDbContext(currentUser1);
        var usersForAccount1 = await context1.Users.ToListAsync();

        // Assert
        usersForAccount1.Should().OnlyContain(u => u.AccountId == account1.Id);
    }

    [Fact]
    public async Task TenantFilter_WorksForExpensesAndIncome()
    {
        // Arrange
        await using var setupContext = _fixture.CreateDbContext();

        var account1 = new Account { Name = "Finance Isolation Account 1" };
        var account2 = new Account { Name = "Finance Isolation Account 2" };
        setupContext.Accounts.AddRange(account1, account2);
        await setupContext.SaveChangesAsync();

        var financeEmail1 = $"finance1-{Guid.NewGuid()}@example.com";
        var financeEmail2 = $"finance2-{Guid.NewGuid()}@example.com";
        var user1 = new ApplicationUser
        {
            AccountId = account1.Id,
            Email = financeEmail1,
            UserName = financeEmail1,
            NormalizedEmail = financeEmail1.ToUpperInvariant(),
            NormalizedUserName = financeEmail1.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = "Owner"
        };
        var user2 = new ApplicationUser
        {
            AccountId = account2.Id,
            Email = financeEmail2,
            UserName = financeEmail2,
            NormalizedEmail = financeEmail2.ToUpperInvariant(),
            NormalizedUserName = financeEmail2.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = "Owner"
        };
        setupContext.Users.AddRange(user1, user2);
        await setupContext.SaveChangesAsync();

        var property1 = new Property { AccountId = account1.Id, Name = "Finance Property 1" };
        var property2 = new Property { AccountId = account2.Id, Name = "Finance Property 2" };
        setupContext.Properties.AddRange(property1, property2);
        await setupContext.SaveChangesAsync();

        var category = await setupContext.ExpenseCategories.FirstAsync();

        var expense1 = new Expense
        {
            AccountId = account1.Id,
            PropertyId = property1.Id,
            CategoryId = category.Id,
            Amount = 100,
            Date = DateOnly.FromDateTime(DateTime.Today),
            CreatedByUserId = user1.Id
        };
        var expense2 = new Expense
        {
            AccountId = account2.Id,
            PropertyId = property2.Id,
            CategoryId = category.Id,
            Amount = 200,
            Date = DateOnly.FromDateTime(DateTime.Today),
            CreatedByUserId = user2.Id
        };
        setupContext.Expenses.AddRange(expense1, expense2);

        var income1 = new Income
        {
            AccountId = account1.Id,
            PropertyId = property1.Id,
            Amount = 1000,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Source = "Rent Account 1",
            CreatedByUserId = user1.Id
        };
        var income2 = new Income
        {
            AccountId = account2.Id,
            PropertyId = property2.Id,
            Amount = 2000,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Source = "Rent Account 2",
            CreatedByUserId = user2.Id
        };
        setupContext.Income.AddRange(income1, income2);
        await setupContext.SaveChangesAsync();

        // Act - Query as Account 1
        var currentUser1 = new TestCurrentUser { AccountId = account1.Id, UserId = user1.Id, IsAuthenticated = true };
        await using var context1 = _fixture.CreateDbContext(currentUser1);
        var expensesForAccount1 = await context1.Expenses.ToListAsync();
        var incomeForAccount1 = await context1.Income.ToListAsync();

        // Assert
        expensesForAccount1.Should().OnlyContain(e => e.AccountId == account1.Id);
        expensesForAccount1.Should().AllSatisfy(e => e.Amount.Should().Be(100));

        incomeForAccount1.Should().OnlyContain(i => i.AccountId == account1.Id);
        incomeForAccount1.Should().AllSatisfy(i => i.Amount.Should().Be(1000));
    }

    [Fact]
    public async Task TenantFilter_NoCurrentUser_ReturnsAllData()
    {
        // Arrange - Create test data
        await using var setupContext = _fixture.CreateDbContext();

        var account = new Account { Name = "No Filter Test Account" };
        setupContext.Accounts.Add(account);
        await setupContext.SaveChangesAsync();

        var property = new Property { AccountId = account.Id, Name = "No Filter Property" };
        setupContext.Properties.Add(property);
        await setupContext.SaveChangesAsync();

        // Act - Query without ICurrentUser (for admin/migration scenarios)
        await using var context = _fixture.CreateDbContext(); // No current user
        var allProperties = await context.Properties.ToListAsync();

        // Assert - Should return data (no tenant filter applied when no user)
        allProperties.Should().Contain(p => p.Name == "No Filter Property");
    }
}
