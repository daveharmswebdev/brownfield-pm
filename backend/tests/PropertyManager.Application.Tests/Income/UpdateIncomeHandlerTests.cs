using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for UpdateIncomeCommandHandler (AC-4.2.2, AC-4.2.3).
/// </summary>
public class UpdateIncomeHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdateIncomeCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();

    public UpdateIncomeHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new UpdateIncomeCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesIncome()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5)));
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
            Source: "Updated Source",
            Description: "Updated description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.Amount.Should().Be(1500.00m);
        income.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(-3)));
        income.Source.Should().Be("Updated Source");
        income.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_AmountChanged_UpdatesAmount()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalAmount = income.Amount;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 2500.50m,
            Date: income.Date,
            Source: income.Source,
            Description: income.Description);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.Amount.Should().Be(2500.50m);
        income.Amount.Should().NotBe(originalAmount);
    }

    [Fact]
    public async Task Handle_ValidUpdate_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalUpdatedAt = income.UpdatedAt;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: income.Date,
            Source: income.Source,
            Description: income.Description);

        // Act
        await Task.Delay(10); // Ensure time passes
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        income.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesCreatedAt()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalCreatedAt = income.CreatedAt;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: income.Date,
            Source: "New source",
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesCreatedByUserId()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalCreatedByUserId = income.CreatedByUserId;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: income.Date,
            Source: "New source",
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.CreatedByUserId.Should().Be(originalCreatedByUserId);
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesPropertyId()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        var originalPropertyId = income.PropertyId;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: income.Date,
            Source: "New source",
            Description: "New description");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.PropertyId.Should().Be(originalPropertyId);
    }

    [Fact]
    public async Task Handle_IncomeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupIncomeDbSet(new List<IncomeEntity>());

        var nonExistentId = Guid.NewGuid();
        var command = new UpdateIncomeCommand(
            Id: nonExistentId,
            Amount: 1000.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "Test",
            Description: null);

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

        var command = new UpdateIncomeCommand(
            Id: otherAccountIncome.Id,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "Attempt to update other account's income",
            Description: null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedIncome_ThrowsNotFoundException()
    {
        // Arrange
        var deletedIncome = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        deletedIncome.DeletedAt = DateTime.UtcNow;
        SetupIncomeDbSet(new List<IncomeEntity> { deletedIncome });

        var command = new UpdateIncomeCommand(
            Id: deletedIncome.Id,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "Attempt to update deleted",
            Description: null);

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

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: 1500.00m,
            Date: income.Date,
            Source: income.Source,
            Description: "Updated");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SourceWithWhitespace_TrimsSource()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: income.Amount,
            Date: income.Date,
            Source: "  Trimmed source  ",
            Description: income.Description);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.Source.Should().Be("Trimmed source");
    }

    [Fact]
    public async Task Handle_DescriptionWithWhitespace_TrimsDescription()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: income.Amount,
            Date: income.Date,
            Source: income.Source,
            Description: "  Trimmed description  ");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.Description.Should().Be("Trimmed description");
    }

    [Fact]
    public async Task Handle_NullSourceAndDescription_SetsNullValues()
    {
        // Arrange
        var income = CreateIncome(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        income.Source = "Original source";
        income.Description = "Original description";
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var command = new UpdateIncomeCommand(
            Id: income.Id,
            Amount: income.Amount,
            Date: income.Date,
            Source: null,
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        income.Source.Should().BeNull();
        income.Description.Should().BeNull();
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
            Source = "Original source",
            Description = "Original description",
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
