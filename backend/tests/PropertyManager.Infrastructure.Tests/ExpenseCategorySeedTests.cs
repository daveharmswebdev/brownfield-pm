using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace PropertyManager.Infrastructure.Tests;

/// <summary>
/// Integration tests for ExpenseCategories seed data (AC: 2.2).
/// </summary>
[Collection("Database")]
public class ExpenseCategorySeedTests
{
    private readonly DatabaseFixture _fixture;

    public ExpenseCategorySeedTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExpenseCategories_SeededWith15Records()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        // Act
        var count = await context.ExpenseCategories.CountAsync();

        // Assert
        count.Should().Be(15);
    }

    [Fact]
    public async Task ExpenseCategories_ContainAllScheduleELineItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        var expectedCategories = new[]
        {
            ("Advertising", "Line 5", 1),
            ("Auto and Travel", "Line 6", 2),
            ("Cleaning and Maintenance", "Line 7", 3),
            ("Commissions", "Line 8", 4),
            ("Insurance", "Line 9", 5),
            ("Legal and Professional Fees", "Line 10", 6),
            ("Management Fees", "Line 11", 7),
            ("Mortgage Interest", "Line 12", 8),
            ("Other Interest", "Line 13", 9),
            ("Repairs", "Line 14", 10),
            ("Supplies", "Line 15", 11),
            ("Taxes", "Line 16", 12),
            ("Utilities", "Line 17", 13),
            ("Depreciation", "Line 18", 14),
            ("Other", "Line 19", 15)
        };

        // Act
        var categories = await context.ExpenseCategories
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        // Assert
        categories.Should().HaveCount(expectedCategories.Length);

        for (int i = 0; i < expectedCategories.Length; i++)
        {
            categories[i].Name.Should().Be(expectedCategories[i].Item1);
            categories[i].ScheduleELine.Should().Be(expectedCategories[i].Item2);
            categories[i].SortOrder.Should().Be(expectedCategories[i].Item3);
        }
    }

    [Fact]
    public async Task ExpenseCategories_HaveValidUuids()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();

        // Act
        var categories = await context.ExpenseCategories.ToListAsync();

        // Assert
        categories.Should().AllSatisfy(c => c.Id.Should().NotBe(Guid.Empty));
    }
}
