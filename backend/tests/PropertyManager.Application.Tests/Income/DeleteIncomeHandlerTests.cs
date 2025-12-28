using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for DeleteIncomeCommandHandler (AC-4.2.5, AC-4.2.6).
/// </summary>
public class DeleteIncomeHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly DeleteIncomeCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();

    public DeleteIncomeHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new DeleteIncomeCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidId_SetsDeletedAtTimestamp()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        income.DeletedAt.Should().BeNull(); // Precondition
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new DeleteIncomeCommand(income.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.DeletedAt.Should().NotBeNull();
        income.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidId_PreservesOtherFields()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1250.50m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
        var originalAmount = income.Amount;
        var originalDate = income.Date;
        var originalSource = income.Source;
        var originalDescription = income.Description;
        var originalPropertyId = income.PropertyId;
        var originalAccountId = income.AccountId;
        var originalCreatedAt = income.CreatedAt;
        var originalCreatedByUserId = income.CreatedByUserId;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new DeleteIncomeCommand(income.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - All other fields unchanged
        income.Amount.Should().Be(originalAmount);
        income.Date.Should().Be(originalDate);
        income.Source.Should().Be(originalSource);
        income.Description.Should().Be(originalDescription);
        income.PropertyId.Should().Be(originalPropertyId);
        income.AccountId.Should().Be(originalAccountId);
        income.CreatedAt.Should().Be(originalCreatedAt);
        income.CreatedByUserId.Should().Be(originalCreatedByUserId);
    }

    [Fact]
    public async Task Handle_IncomeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupIncomeDbSet(new List<IncomeEntity>());

        var nonExistentId = Guid.NewGuid();
        var command = new DeleteIncomeCommand(nonExistentId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_WrongAccount_ThrowsNotFoundException()
    {
        // Arrange - income belongs to different account (simulated by global query filter)
        var otherAccountIncome = CreateIncome(_otherAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        // Don't add to mock - simulates global query filter excluding it
        SetupIncomeDbSet(new List<IncomeEntity>());

        var command = new DeleteIncomeCommand(otherAccountIncome.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ThrowsNotFoundException()
    {
        // Arrange
        var deletedIncome = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        deletedIncome.DeletedAt = DateTime.UtcNow.AddDays(-1);
        SetupIncomeDbSet(new List<IncomeEntity> { deletedIncome });

        var command = new DeleteIncomeCommand(deletedIncome.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new DeleteIncomeCommand(income.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private IncomeEntity CreateIncome(Guid accountId, decimal amount, DateOnly date)
    {
        return new IncomeEntity
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PropertyId = _testPropertyId,
            Amount = amount,
            Date = date,
            Source = "Test source",
            Description = "Test income",
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    private void SetupIncomeDbSet(List<IncomeEntity> incomeEntries)
    {
        // Filter to simulate global query filter
        var filteredIncome = incomeEntries
            .Where(i => i.AccountId == _testAccountId && i.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredIncome.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Income).Returns(mockDbSet.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
