using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for DeleteExpenseCommandHandler (AC-3.3.1, AC-3.3.3).
/// </summary>
public class DeleteExpenseHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly DeleteExpenseCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    public DeleteExpenseHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new DeleteExpenseCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidId_SetsDeletedAtTimestamp()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        expense.DeletedAt.Should().BeNull(); // Precondition
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new DeleteExpenseCommand(expense.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        expense.DeletedAt.Should().NotBeNull();
        expense.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidId_PreservesOtherFields()
    {
        // Arrange
        var expense = CreateExpense(_testAccountId, 125.50m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
        var originalAmount = expense.Amount;
        var originalDate = expense.Date;
        var originalDescription = expense.Description;
        var originalCategoryId = expense.CategoryId;
        var originalPropertyId = expense.PropertyId;
        var originalAccountId = expense.AccountId;
        var originalCreatedAt = expense.CreatedAt;
        var originalCreatedByUserId = expense.CreatedByUserId;
        SetupExpensesDbSet(new List<Expense> { expense });

        var command = new DeleteExpenseCommand(expense.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - All other fields unchanged
        expense.Amount.Should().Be(originalAmount);
        expense.Date.Should().Be(originalDate);
        expense.Description.Should().Be(originalDescription);
        expense.CategoryId.Should().Be(originalCategoryId);
        expense.PropertyId.Should().Be(originalPropertyId);
        expense.AccountId.Should().Be(originalAccountId);
        expense.CreatedAt.Should().Be(originalCreatedAt);
        expense.CreatedByUserId.Should().Be(originalCreatedByUserId);
    }

    [Fact]
    public async Task Handle_ExpenseNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupExpensesDbSet(new List<Expense>());

        var nonExistentId = Guid.NewGuid();
        var command = new DeleteExpenseCommand(nonExistentId);

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

        var command = new DeleteExpenseCommand(otherAccountExpense.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ThrowsNotFoundException()
    {
        // Arrange
        var deletedExpense = CreateExpense(_testAccountId, 100.00m, DateOnly.FromDateTime(DateTime.Today));
        deletedExpense.DeletedAt = DateTime.UtcNow.AddDays(-1);
        SetupExpensesDbSet(new List<Expense> { deletedExpense });

        var command = new DeleteExpenseCommand(deletedExpense.Id);

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

        var command = new DeleteExpenseCommand(expense.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            .Where(e => e.AccountId == _testAccountId && e.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredExpenses.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Expenses).Returns(mockDbSet.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
