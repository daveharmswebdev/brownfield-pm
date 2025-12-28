using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for GetExpenseTotalsHandler (AC-3.5.2, AC-3.5.4).
/// </summary>
public class GetExpenseTotalsHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly GetExpenseTotalsHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testProperty1Id = Guid.NewGuid();
    private readonly Guid _testProperty2Id = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    public GetExpenseTotalsHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new GetExpenseTotalsHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithExpenses_ReturnsTotalExpenses()
    {
        // Arrange
        var year = 2024;
        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Property 1", 100m, new DateOnly(year, 1, 15)),
            CreateExpense(_testProperty1Id, "Property 1", 50m, new DateOnly(year, 3, 20)),
            CreateExpense(_testProperty2Id, "Property 2", 200m, new DateOnly(year, 6, 10))
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalExpenses.Should().Be(350m);
        result.Year.Should().Be(year);
    }

    [Fact]
    public async Task Handle_WithExpenses_ReturnsPerPropertyBreakdown()
    {
        // Arrange
        var year = 2024;
        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Property 1", 100m, new DateOnly(year, 1, 15)),
            CreateExpense(_testProperty1Id, "Property 1", 50m, new DateOnly(year, 3, 20)),
            CreateExpense(_testProperty2Id, "Property 2", 200m, new DateOnly(year, 6, 10))
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ByProperty.Should().HaveCount(2);
        result.ByProperty.Should().Contain(p => p.PropertyId == _testProperty1Id && p.Total == 150m);
        result.ByProperty.Should().Contain(p => p.PropertyId == _testProperty2Id && p.Total == 200m);
    }

    [Fact]
    public async Task Handle_NoExpenses_ReturnsZeroTotal()
    {
        // Arrange
        var year = 2024;
        SetupExpensesDbSet(new List<Expense>());

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalExpenses.Should().Be(0m);
        result.Year.Should().Be(year);
        result.ByProperty.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersExpensesByYear()
    {
        // Arrange
        var year = 2024;
        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Property 1", 100m, new DateOnly(2024, 1, 15)),
            CreateExpense(_testProperty1Id, "Property 1", 50m, new DateOnly(2023, 3, 20)), // Previous year
            CreateExpense(_testProperty2Id, "Property 2", 200m, new DateOnly(2024, 6, 10))
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalExpenses.Should().Be(300m); // Only 2024 expenses
    }

    [Fact]
    public async Task Handle_IncludesExpensesOnYearBoundaries()
    {
        // Arrange
        var year = 2024;
        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Property 1", 100m, new DateOnly(2024, 1, 1)), // Year start
            CreateExpense(_testProperty1Id, "Property 1", 50m, new DateOnly(2024, 12, 31)) // Year end
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalExpenses.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_ExcludesDeletedExpenses()
    {
        // Arrange
        var year = 2024;
        var deletedExpense = CreateExpense(_testProperty1Id, "Property 1", 100m, new DateOnly(year, 1, 15));
        deletedExpense.DeletedAt = DateTime.UtcNow;

        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Property 1", 50m, new DateOnly(year, 3, 20)),
            deletedExpense
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalExpenses.Should().Be(50m); // Only non-deleted expense
    }

    [Fact]
    public async Task Handle_ReturnsPropertyNamesInBreakdown()
    {
        // Arrange
        var year = 2024;
        var expenses = new List<Expense>
        {
            CreateExpense(_testProperty1Id, "Main House", 100m, new DateOnly(year, 1, 15))
        };
        SetupExpensesDbSet(expenses);

        var query = new GetExpenseTotalsQuery(year);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ByProperty.Should().ContainSingle(p => p.PropertyName == "Main House");
    }

    private Expense CreateExpense(Guid propertyId, string propertyName, decimal amount, DateOnly date)
    {
        var property = new Property
        {
            Id = propertyId,
            AccountId = _testAccountId,
            Name = propertyName,
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
            Description = "Test expense",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetupExpensesDbSet(List<Expense> expenses)
    {
        // Filter to simulate global query filter
        var filteredExpenses = expenses
            .Where(e => e.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredExpenses.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Expenses).Returns(mockDbSet.Object);
    }
}
