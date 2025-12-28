using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Entities;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for GetAllPropertiesQueryHandler (AC-2.2.2, AC-2.2.4, AC-2.2.6).
/// </summary>
public class GetAllPropertiesHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetAllPropertiesQueryHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();

    public GetAllPropertiesHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new GetAllPropertiesQueryHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoProperties_ReturnsEmptyList()
    {
        // Arrange
        var properties = new List<Property>();
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithProperties_ReturnsAllForAccount()
    {
        // Arrange
        var properties = new List<Property>
        {
            CreateProperty(_testAccountId, "Property A", "Austin", "TX"),
            CreateProperty(_testAccountId, "Property B", "Dallas", "TX"),
            CreateProperty(_otherAccountId, "Other Account Property", "Houston", "TX")
        };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(p => p.Name == "Property A" || p.Name == "Property B");
    }

    [Fact]
    public async Task Handle_PropertiesSortedByNameAlphabetically()
    {
        // Arrange
        var properties = new List<Property>
        {
            CreateProperty(_testAccountId, "Zebra Property", "Austin", "TX"),
            CreateProperty(_testAccountId, "Alpha Property", "Dallas", "TX"),
            CreateProperty(_testAccountId, "Middle Property", "Houston", "TX")
        };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha Property");
        result.Items[1].Name.Should().Be("Middle Property");
        result.Items[2].Name.Should().Be("Zebra Property");
    }

    [Fact]
    public async Task Handle_ExcludesDeletedProperties()
    {
        // Arrange
        var activeProperty = CreateProperty(_testAccountId, "Active Property", "Austin", "TX");
        var deletedProperty = CreateProperty(_testAccountId, "Deleted Property", "Dallas", "TX");
        deletedProperty.DeletedAt = DateTime.UtcNow;

        var properties = new List<Property> { activeProperty, deletedProperty };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Active Property");
    }

    [Fact]
    public async Task Handle_ReturnsCorrectPropertySummaryDto()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        property.Street = "123 Main Street";
        property.ZipCode = "78701";

        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.Id.Should().Be(property.Id);
        dto.Name.Should().Be("Test Property");
        dto.Street.Should().Be("123 Main Street");
        dto.City.Should().Be("Austin");
        dto.State.Should().Be("TX");
        dto.ZipCode.Should().Be("78701");
        dto.ExpenseTotal.Should().Be(0m);
        dto.IncomeTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_MultiTenantIsolation_OnlyReturnsCurrentAccountProperties()
    {
        // Arrange
        var properties = new List<Property>
        {
            CreateProperty(_testAccountId, "My Property 1", "Austin", "TX"),
            CreateProperty(_testAccountId, "My Property 2", "Dallas", "TX"),
            CreateProperty(_otherAccountId, "Other User Property 1", "Houston", "TX"),
            CreateProperty(_otherAccountId, "Other User Property 2", "San Antonio", "TX"),
            CreateProperty(Guid.NewGuid(), "Third User Property", "El Paso", "TX")
        };
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Name.StartsWith("My Property"));
    }

    [Fact]
    public async Task Handle_WithYearParameter_AcceptsYearFilter()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Property A", "Austin", "TX");
        var properties = new List<Property> { property };

        var expenses = new List<Expense>
        {
            CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(2024, 3, 15)),
            CreateExpense(_testAccountId, property.Id, 300m, new DateOnly(2023, 6, 10))
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery(Year: 2024);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ExpenseTotal.Should().Be(500m);
        result.Items[0].IncomeTotal.Should().Be(0m);
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
            CreateExpense(_testAccountId, property.Id, 500.50m, new DateOnly(currentYear, 1, 15)),
            CreateExpense(_testAccountId, property.Id, 300.25m, new DateOnly(currentYear, 2, 20)),
            CreateExpense(_testAccountId, property.Id, 150.00m, new DateOnly(currentYear, 3, 10))
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ExpenseTotal.Should().Be(950.75m);
    }

    [Fact]
    public async Task Handle_WithYearFilter_FiltersExpensesByYear()
    {
        // Arrange
        var property1 = CreateProperty(_testAccountId, "Property 1", "Austin", "TX");
        var property2 = CreateProperty(_testAccountId, "Property 2", "Dallas", "TX");
        var properties = new List<Property> { property1, property2 };

        var expenses = new List<Expense>
        {
            // Property 1 - 2024 expenses
            CreateExpense(_testAccountId, property1.Id, 500m, new DateOnly(2024, 3, 15)),
            CreateExpense(_testAccountId, property1.Id, 300m, new DateOnly(2024, 6, 10)),
            // Property 1 - 2023 expenses (should be filtered out)
            CreateExpense(_testAccountId, property1.Id, 1000m, new DateOnly(2023, 5, 20)),
            // Property 2 - 2024 expenses
            CreateExpense(_testAccountId, property2.Id, 750m, new DateOnly(2024, 8, 5)),
            // Property 2 - 2025 expenses (should be filtered out)
            CreateExpense(_testAccountId, property2.Id, 2000m, new DateOnly(2025, 1, 1))
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery(Year: 2024);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        var p1 = result.Items.First(p => p.Name == "Property 1");
        var p2 = result.Items.First(p => p.Name == "Property 2");
        p1.ExpenseTotal.Should().Be(800m); // 500 + 300
        p2.ExpenseTotal.Should().Be(750m);
    }

    [Fact]
    public async Task Handle_WithExpenses_ExcludesSoftDeletedExpenses()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Austin", "TX");
        var properties = new List<Property> { property };

        var currentYear = DateTime.UtcNow.Year;
        var activeExpense = CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 3, 15));
        var deletedExpense = CreateExpense(_testAccountId, property.Id, 300m, new DateOnly(currentYear, 6, 10));
        deletedExpense.DeletedAt = DateTime.UtcNow;

        var expenses = new List<Expense> { activeExpense, deletedExpense };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ExpenseTotal.Should().Be(500m);
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
            CreateExpense(_testAccountId, property.Id, 500m, new DateOnly(currentYear, 3, 15)),
            CreateExpense(_otherAccountId, property.Id, 1000m, new DateOnly(currentYear, 6, 10)) // Different account
        };

        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(expenses);
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ExpenseTotal.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_ReturnsAllPropertiesWithoutPagination()
    {
        // Arrange - Create 20 properties to verify no pagination
        var properties = Enumerable.Range(1, 20)
            .Select(i => CreateProperty(_testAccountId, $"Property {i:D2}", "City", "TX"))
            .ToList();
        SetupPropertiesDbSet(properties);
        SetupExpensesDbSet(new List<Expense>());
        SetupIncomeDbSet(new List<IncomeEntity>());
        var query = new GetAllPropertiesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(20);
        result.TotalCount.Should().Be(20);
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

    private Expense CreateExpense(Guid accountId, Guid propertyId, decimal amount, DateOnly date)
    {
        return new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PropertyId = propertyId,
            CategoryId = Guid.NewGuid(),
            Amount = amount,
            Date = date,
            Description = "Test expense",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
