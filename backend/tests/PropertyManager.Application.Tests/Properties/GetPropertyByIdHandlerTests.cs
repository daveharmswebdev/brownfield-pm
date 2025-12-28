using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Entities;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for GetPropertyByIdQueryHandler (AC-2.3.2, AC-2.3.5, AC-2.3.6).
/// </summary>
public class GetPropertyByIdHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetPropertyByIdQueryHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();

    public GetPropertyByIdHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new GetPropertyByIdQueryHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPropertyId_ReturnsPropertyDetailDto()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        property.Street = "123 Main Street";
        property.ZipCode = "78701";
        property.CreatedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        property.UpdatedAt = new DateTime(2025, 1, 20, 14, 45, 0, DateTimeKind.Utc);

        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(property.Id);
        result.Name.Should().Be("Test Property");
        result.Street.Should().Be("123 Main Street");
        result.City.Should().Be("Austin");
        result.State.Should().Be("TX");
        result.ZipCode.Should().Be("78701");
        result.CreatedAt.Should().Be(property.CreatedAt);
        result.UpdatedAt.Should().Be(property.UpdatedAt);
    }

    [Fact]
    public async Task Handle_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var properties = new List<Property>();
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var nonExistentId = Guid.NewGuid();
        var query = new GetPropertyByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PropertyBelongsToDifferentAccount_ReturnsNull()
    {
        // Arrange
        var otherAccountProperty = CreateProperty(_otherAccountId, "Other Account Property", "Houston", "TX");
        var properties = new List<Property> { otherAccountProperty };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(otherAccountProperty.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletedProperty_ReturnsNull()
    {
        // Arrange
        var deletedProperty = CreateProperty(_testAccountId, "Deleted Property", "Dallas", "TX");
        deletedProperty.DeletedAt = DateTime.UtcNow;

        var properties = new List<Property> { deletedProperty };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(deletedProperty.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoExpenses_ReturnsZeroExpenseTotal()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpenseTotal.Should().Be(0m);
        result.IncomeTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithNoExpenses_ReturnsEmptyRecentExpensesArray()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RecentExpenses.Should().BeEmpty();
        result.RecentIncome.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithExpenses_ReturnsCorrectExpenseTotal()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 500.50m, new DateOnly(currentYear, 1, 15), "Plumbing repair"),
            CreateExpense(_testAccountId, property.Id, 300.25m, new DateOnly(currentYear, 2, 20), "Electrical work"),
            CreateExpense(_testAccountId, property.Id, 150.00m, new DateOnly(currentYear, 3, 10), "Lawn maintenance")
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpenseTotal.Should().Be(950.75m);
    }

    [Fact]
    public async Task Handle_WithExpenses_ReturnsRecentExpensesOrderedByDateDescending()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 100m, new DateOnly(currentYear, 1, 15), "First"),
            CreateExpense(_testAccountId, property.Id, 200m, new DateOnly(currentYear, 3, 20), "Third"),
            CreateExpense(_testAccountId, property.Id, 150m, new DateOnly(currentYear, 2, 10), "Second")
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RecentExpenses.Should().HaveCount(3);
        result.RecentExpenses[0].Description.Should().Be("Third");
        result.RecentExpenses[0].Amount.Should().Be(200m);
        result.RecentExpenses[0].Date.Should().Be(new DateTime(currentYear, 3, 20));
        result.RecentExpenses[1].Description.Should().Be("Second");
        result.RecentExpenses[2].Description.Should().Be("First");
    }

    [Fact]
    public async Task Handle_WithMoreThan5Expenses_ReturnsOnly5MostRecent()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 100m, new DateOnly(currentYear, 1, 1), "First"),
            CreateExpense(_testAccountId, property.Id, 200m, new DateOnly(currentYear, 2, 1), "Second"),
            CreateExpense(_testAccountId, property.Id, 300m, new DateOnly(currentYear, 3, 1), "Third"),
            CreateExpense(_testAccountId, property.Id, 400m, new DateOnly(currentYear, 4, 1), "Fourth"),
            CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 5, 1), "Fifth"),
            CreateExpense(_testAccountId, property.Id, 600m, new DateOnly(currentYear, 6, 1), "Sixth"),
            CreateExpense(_testAccountId, property.Id, 700m, new DateOnly(currentYear, 7, 1), "Seventh")
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RecentExpenses.Should().HaveCount(5);
        result.RecentExpenses[0].Description.Should().Be("Seventh");
        result.RecentExpenses[1].Description.Should().Be("Sixth");
        result.RecentExpenses[2].Description.Should().Be("Fifth");
        result.RecentExpenses[3].Description.Should().Be("Fourth");
        result.RecentExpenses[4].Description.Should().Be("Third");
        // First and Second should NOT be included
    }

    [Fact]
    public async Task Handle_WithExpenses_ExcludesSoftDeletedExpenses()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var activeExpense = CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 3, 15), "Active");
        var deletedExpense = CreateExpense(_testAccountId, property.Id, 300m, new DateOnly(currentYear, 6, 10), "Deleted");
        deletedExpense.DeletedAt = DateTime.UtcNow;

        var expenses = new List<Expense> { activeExpense, deletedExpense };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpenseTotal.Should().Be(500m);
        result.RecentExpenses.Should().HaveCount(1);
        result.RecentExpenses[0].Description.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WithExpenses_OnlyIncludesCurrentAccountExpenses()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 3, 15), "My expense"),
            CreateExpense(_otherAccountId, property.Id, 1000m, new DateOnly(currentYear, 6, 10), "Other account expense")
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpenseTotal.Should().Be(500m);
        result.RecentExpenses.Should().HaveCount(1);
        result.RecentExpenses[0].Description.Should().Be("My expense");
    }

    [Fact]
    public async Task Handle_WithExpensesForCurrentYear_FiltersCorrectly()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 3, 15), "Current year"),
            CreateExpense(_testAccountId, property.Id, 300m, new DateOnly(currentYear - 1, 6, 10), "Last year"),
            CreateExpense(_testAccountId, property.Id, 200m, new DateOnly(currentYear + 1, 1, 1), "Next year")
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExpenseTotal.Should().Be(500m);
        result.RecentExpenses.Should().HaveCount(1);
        result.RecentExpenses[0].Description.Should().Be("Current year");
    }

    [Fact]
    public async Task Handle_WithMultipleProperties_ReturnsOnlyRequestedProperty()
    {
        // Arrange
        var property1 = CreateProperty(_testAccountId, "Property 1", "Austin", "TX");
        var property2 = CreateProperty(_testAccountId, "Property 2", "Dallas", "TX");
        var property3 = CreateProperty(_testAccountId, "Property 3", "Houston", "TX");

        var properties = new List<Property> { property1, property2, property3 };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(property2.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(property2.Id);
        result.Name.Should().Be("Property 2");
    }

    [Fact]
    public async Task Handle_EmptyGuid_ReturnsNull()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetPropertyByIdQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private Property CreateProperty(Guid accountId, string name, string city, string state)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name,
            Street = "123 Test Street",
            City = city,
            State = state,
            ZipCode = "12345",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetupPropertiesDbSet(List<Property> properties)
    {
        var mockDbSet = properties.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Properties).Returns(mockDbSet.Object);
    }

    private void SetupExpensesDbSet(List<Expense> expenses)
    {
        var mockDbSet = expenses.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Expenses).Returns(mockDbSet.Object);
    }

    private void SetupIncomeDbSet(List<IncomeEntity> income)
    {
        var mockDbSet = income.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Income).Returns(mockDbSet.Object);
    }

    private Expense CreateExpense(Guid accountId, Guid propertyId, decimal amount, DateOnly date, string description)
    {
        return new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PropertyId = propertyId,
            CategoryId = Guid.NewGuid(),
            Amount = amount,
            Date = date,
            Description = description,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
