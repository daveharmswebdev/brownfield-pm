using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for GetExpenseQueryHandler (AC-3.2.1, AC-3.2.2).
/// </summary>
public class GetExpenseHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly GetExpenseQueryHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    public GetExpenseHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new GetExpenseQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsExpense()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expense.Id);
        result.Amount.Should().Be(expense.Amount);
        result.Date.Should().Be(expense.Date);
        result.Description.Should().Be(expense.Description);
        result.PropertyId.Should().Be(expense.PropertyId);
        result.CategoryId.Should().Be(expense.CategoryId);
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsPropertyName()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PropertyName.Should().Be("Test Property");
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsCategoryName()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.CategoryName.Should().Be("Repairs");
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsScheduleELine()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ScheduleELine.Should().Be("Line 14");
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupExpensesDbSet(new List<Expense>());

        var nonExistentId = Guid.NewGuid();
        var query = new GetExpenseQuery(nonExistentId);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_DeletedExpense_ThrowsNotFoundException()
    {
        // Arrange
        var deletedExpense = CreateExpenseWithRelations();
        deletedExpense.DeletedAt = DateTime.UtcNow;
        SetupExpensesDbSet(new List<Expense> { deletedExpense });

        var query = new GetExpenseQuery(deletedExpense.Id);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongAccount_ThrowsNotFoundException()
    {
        // Arrange - with global query filter, other account expenses not visible
        SetupExpensesDbSet(new List<Expense>());

        var otherAccountExpenseId = Guid.NewGuid();
        var query = new GetExpenseQuery(otherAccountExpenseId);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsCreatedAt()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.CreatedAt.Should().Be(expense.CreatedAt);
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsReceiptId()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        expense.ReceiptId = Guid.NewGuid();
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ReceiptId.Should().Be(expense.ReceiptId);
    }

    [Fact]
    public async Task Handle_NullReceiptId_ReturnsNullReceiptId()
    {
        // Arrange
        var expense = CreateExpenseWithRelations();
        expense.ReceiptId = null;
        SetupExpensesDbSet(new List<Expense> { expense });

        var query = new GetExpenseQuery(expense.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ReceiptId.Should().BeNull();
    }

    private Expense CreateExpenseWithRelations()
    {
        var property = new Property
        {
            Id = _testPropertyId,
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
            PropertyId = _testPropertyId,
            Property = property,
            CategoryId = _testCategoryId,
            Category = category,
            Amount = 150.50m,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            Description = "Test expense",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
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
