using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for CheckDuplicateExpenseHandler (AC-3.6.1, AC-3.6.5).
/// Tests duplicate detection algorithm:
/// - Same property + same amount + date within ±1 day = duplicate
/// - Soft-deleted expenses should be excluded
/// </summary>
public class CheckDuplicateExpenseHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly CheckDuplicateExpenseHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();
    private readonly Guid _testProperty2Id = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    public CheckDuplicateExpenseHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new CheckDuplicateExpenseHandler(_dbContextMock.Object);
    }

    /// <summary>
    /// AC-3.6.1: Duplicate detection triggers when same property + amount + date within 24 hours.
    /// </summary>
    [Fact]
    public async Task Handle_MatchingExpense_ReturnsIsDuplicateTrue()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1), "Test description");
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeTrue();
        result.ExistingExpense.Should().NotBeNull();
        result.ExistingExpense!.Id.Should().Be(existingExpense.Id);
        result.ExistingExpense.Amount.Should().Be(100m);
        result.ExistingExpense.Description.Should().Be("Test description");
    }

    /// <summary>
    /// AC-3.6.5: No duplicate when no matching expense exists.
    /// </summary>
    [Fact]
    public async Task Handle_NoMatchingExpense_ReturnsIsDuplicateFalse()
    {
        // Arrange
        SetupExpensesDbSet(new List<Expense>());

        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeFalse();
        result.ExistingExpense.Should().BeNull();
    }

    /// <summary>
    /// AC-3.6.5: Same property, different amount - no duplicate.
    /// </summary>
    [Fact]
    public async Task Handle_SamePropertyDifferentAmount_ReturnsNotDuplicate()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1));
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 150m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeFalse();
    }

    /// <summary>
    /// AC-3.6.5: Different property, same amount - no duplicate.
    /// </summary>
    [Fact]
    public async Task Handle_SameAmountDifferentProperty_ReturnsNotDuplicate()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1));
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        var query = new CheckDuplicateExpenseQuery(_testProperty2Id, 100m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeFalse();
    }

    /// <summary>
    /// AC-3.6.1: Date within 24 hours (±1 day) returns duplicate.
    /// Edge case: Dec 1 vs Dec 2 = duplicate warning.
    /// </summary>
    [Fact]
    public async Task Handle_DateWithin24Hours_ReturnsDuplicate()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1));
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        // Query for Dec 2 - should detect Dec 1 expense as duplicate
        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 2));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeTrue();
        result.ExistingExpense.Should().NotBeNull();
    }

    /// <summary>
    /// AC-3.6.5: Date more than 24 hours apart returns no duplicate.
    /// Edge case: Dec 1 vs Dec 3 = no warning.
    /// </summary>
    [Fact]
    public async Task Handle_DateMoreThan24HoursApart_ReturnsNotDuplicate()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1));
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        // Query for Dec 3 - should NOT detect Dec 1 expense as duplicate
        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 3));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeFalse();
    }

    /// <summary>
    /// Soft-deleted expenses should be excluded from duplicate check.
    /// </summary>
    [Fact]
    public async Task Handle_DeletedExpense_IsIgnored()
    {
        // Arrange
        var deletedExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 1));
        deletedExpense.DeletedAt = DateTime.UtcNow;
        SetupExpensesDbSet(new List<Expense> { deletedExpense });

        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeFalse();
    }

    /// <summary>
    /// Date one day before (previous day) should return duplicate.
    /// </summary>
    [Fact]
    public async Task Handle_DateOneDayBefore_ReturnsDuplicate()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 100m, new DateOnly(2024, 12, 2));
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        // Query for Dec 1 - should detect Dec 2 expense as duplicate
        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 100m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeTrue();
    }

    /// <summary>
    /// Returns expense details for dialog display (AC-3.6.2).
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsExpenseDetailsForDialog()
    {
        // Arrange
        var existingExpense = CreateExpense(_testPropertyId, 127.50m, new DateOnly(2024, 12, 1), "Home Depot - Faucet");
        SetupExpensesDbSet(new List<Expense> { existingExpense });

        var query = new CheckDuplicateExpenseQuery(_testPropertyId, 127.50m, new DateOnly(2024, 12, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsDuplicate.Should().BeTrue();
        result.ExistingExpense.Should().NotBeNull();
        result.ExistingExpense!.Date.Should().Be(new DateOnly(2024, 12, 1));
        result.ExistingExpense.Amount.Should().Be(127.50m);
        result.ExistingExpense.Description.Should().Be("Home Depot - Faucet");
    }

    private Expense CreateExpense(Guid propertyId, decimal amount, DateOnly date, string? description = null)
    {
        var property = new Property
        {
            Id = propertyId,
            AccountId = _testAccountId,
            Name = "Test Property",
            Street = "123 Test St",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        var category = new ExpenseCategory
        {
            Id = _testCategoryId,
            Name = "Repairs",
            ScheduleELine = "Line 14",
            SortOrder = 1
        };

        return new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = propertyId,
            Property = property,
            CategoryId = _testCategoryId,
            Category = category,
            Amount = amount,
            Date = date,
            Description = description,
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetupExpensesDbSet(List<Expense> expenses)
    {
        // Filter to simulate global query filter (exclude deleted expenses)
        var filteredExpenses = expenses
            .Where(e => e.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredExpenses.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Expenses).Returns(mockDbSet.Object);
    }
}
