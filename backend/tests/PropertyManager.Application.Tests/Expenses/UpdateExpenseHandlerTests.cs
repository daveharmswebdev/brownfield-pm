using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for UpdateExpenseCommandHandler (AC-3.2.1, AC-3.2.3).
/// </summary>
public class UpdateExpenseHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdateExpenseCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();
    private readonly Guid _newCategoryId = Guid.NewGuid();

    public UpdateExpenseHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new UpdateExpenseCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        // Setup expense categories
        SetupExpenseCategories();
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesExpense()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
            CategoryId: _newCategoryId,
            Description: "Updated description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.Amount.Should().Be(150.00m);
        expense.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-3)));
        expense.CategoryId.Should().Be(_newCategoryId);
        expense.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_AmountChanged_UpdatesAmount()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalAmount = expense.Amount;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 250.50m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: expense.Description);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.Amount.Should().Be(250.50m);
        expense.Amount.Should().NotBe(originalAmount);
    }

    [Fact]
    public async Task Handle_ValidUpdate_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalUpdatedAt = expense.UpdatedAt;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: expense.Description);

        // Act
        await Task.Delay(10); // Ensure time passes
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        expense.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesCreatedAt()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalCreatedAt = expense.CreatedAt;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesCreatedByUserId()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalCreatedByUserId = expense.CreatedByUserId;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.CreatedByUserId.Should().Be(originalCreatedByUserId);
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesPropertyId()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalPropertyId = expense.PropertyId;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.PropertyId.Should().Be(originalPropertyId);
    }

    [Fact]
    public async Task Handle_ExpenseNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupExpensesDbSet(new List<Expense>());

        var nonExistentId = Guid.NewGuid();
        var command = new UpdateExpenseCommand(
            Id: nonExistentId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _testCategoryId,
            Description: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_WrongAccount_ThrowsNotFoundException()
    {
        // Arrange - expense belongs to different account (simulated by global query filter)
        // With global query filter, expenses from other accounts are not visible
        var otherAccountExpense = CreateExpense(_otherAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        // Don't add to mock - simulates global query filter excluding it
        SetupExpensesDbSet(new List<Expense>());

        var command = new UpdateExpenseCommand(
            Id: otherAccountExpense.Id,
            Amount: 150.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _testCategoryId,
            Description: "Attempt to update other account's expense");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidCategory_ThrowsNotFoundException()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupExpensesDbSet(new List<Expense> { expense });

        var invalidCategoryId = Guid.NewGuid();
        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: invalidCategoryId,
            Description: "Test");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{invalidCategoryId}*");
    }

    [Fact]
    public async Task Handle_DeletedExpense_ThrowsNotFoundException()
    {
        // Arrange
        var deletedExpense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        deletedExpense.DeletedAt = DateTime.UtcNow;
        SetupExpensesDbSet(new List<Expense> { deletedExpense });

        var command = new UpdateExpenseCommand(
            Id: deletedExpense.Id,
            Amount: 150.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _testCategoryId,
            Description: "Attempt to update deleted");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: 150.00m,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: "Updated");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DescriptionWithWhitespace_TrimsDescription()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: expense.Amount,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: "  Trimmed description  ");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.Description.Should().Be("Trimmed description");
    }

    [Fact]
    public async Task Handle_NullDescription_SetsNullDescription()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        expense.Description = "Original description";
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            Amount: expense.Amount,
            Date: expense.Date,
            CategoryId: expense.CategoryId,
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.Description.Should().BeNull();
    }

    private Expense CreateExpense(Guid accountId, decimal amount, DateOnly date)
    {
        return new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PropertyId = _testPropertyId,
            CategoryId = _testCategoryId,
            Amount = amount,
            Date = date,
            Description = "Original description",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    private void SetupExpensesDbSet(List<Expense> expenses)
    {
        // Filter to simulate global query filter
        var filteredExpenses = expenses
            .Where(e => e.AccountId == _testAccountId && e.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredExpenses.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Expenses).Returns(mockDbSet.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private void SetupExpenseCategories()
    {
        var categories = new List<ExpenseCategory>
        {
            new() { Id = _testCategoryId, Name = "Repairs", SortOrder = 1 },
            new() { Id = _newCategoryId, Name = "Insurance", SortOrder = 2 }
        };

        var mockDbSet = categories.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ExpenseCategories).Returns(mockDbSet.Object);
    }
}
