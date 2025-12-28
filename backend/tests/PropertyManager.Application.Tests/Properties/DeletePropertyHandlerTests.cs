using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for DeletePropertyCommandHandler (AC-2.5.2, AC-2.5.3).
/// Tests soft delete behavior and data preservation.
/// </summary>
public class DeletePropertyHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<DeletePropertyCommandHandler>> _loggerMock;
    private readonly DeletePropertyCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();

    public DeletePropertyHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<DeletePropertyCommandHandler>>();

        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new DeletePropertyCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProperty_SetsDeletedAt()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property");
        property.DeletedAt.Should().BeNull(); // Verify precondition
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(property.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.DeletedAt.Should().NotBeNull();
        property.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidProperty_CallsSaveChanges()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(property.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropertyNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var properties = new List<Property>();
        SetupPropertiesDbSet(properties);

        var nonExistentId = Guid.NewGuid();
        var command = new DeletePropertyCommand(nonExistentId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_PropertyBelongsToOtherAccount_ThrowsNotFoundException()
    {
        // Arrange
        var otherAccountProperty = CreateProperty(_otherAccountId, "Other Account Property");
        var properties = new List<Property> { otherAccountProperty };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(otherAccountProperty.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeletedProperty_ThrowsNotFoundException()
    {
        // Arrange
        var deletedProperty = CreateProperty(_testAccountId, "Deleted Property");
        deletedProperty.DeletedAt = DateTime.UtcNow.AddDays(-1);
        var properties = new List<Property> { deletedProperty };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(deletedProperty.Id);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PropertyWithExpenses_DoesNotCascadeDelete()
    {
        // Arrange (AC-2.5.3 - expenses must be preserved)
        var property = CreateProperty(_testAccountId, "Property With Expenses");
        var expense1 = new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = property.Id,
            Description = "Test Expense 1",
            Amount = 100m,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DeletedAt = null
        };
        var expense2 = new Expense
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = property.Id,
            Description = "Test Expense 2",
            Amount = 200m,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DeletedAt = null
        };
        property.Expenses = new List<Expense> { expense1, expense2 };

        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(property.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Property should be deleted but expenses should remain untouched
        property.DeletedAt.Should().NotBeNull();
        expense1.DeletedAt.Should().BeNull("Expenses should be preserved for tax records");
        expense2.DeletedAt.Should().BeNull("Expenses should be preserved for tax records");
    }

    [Fact]
    public async Task Handle_PropertyWithIncome_DoesNotCascadeDelete()
    {
        // Arrange (AC-2.5.3 - income must be preserved)
        var property = CreateProperty(_testAccountId, "Property With Income");
        var income1 = new IncomeEntity
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = property.Id,
            Description = "Rent Payment",
            Amount = 1500m,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DeletedAt = null
        };
        property.Income = new List<IncomeEntity> { income1 };

        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(property.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Property should be deleted but income should remain untouched
        property.DeletedAt.Should().NotBeNull();
        income1.DeletedAt.Should().BeNull("Income should be preserved for tax records");
    }

    [Fact]
    public async Task Handle_DoesNotChangeOtherPropertyFields()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property");
        var originalName = property.Name;
        var originalStreet = property.Street;
        var originalCity = property.City;
        var originalState = property.State;
        var originalZipCode = property.ZipCode;
        var originalAccountId = property.AccountId;
        var originalCreatedAt = property.CreatedAt;

        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new DeletePropertyCommand(property.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Only DeletedAt should change
        property.Name.Should().Be(originalName);
        property.Street.Should().Be(originalStreet);
        property.City.Should().Be(originalCity);
        property.State.Should().Be(originalState);
        property.ZipCode.Should().Be(originalZipCode);
        property.AccountId.Should().Be(originalAccountId);
        property.CreatedAt.Should().Be(originalCreatedAt);
    }

    private Property CreateProperty(Guid accountId, string name)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name,
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            DeletedAt = null,
            Expenses = new List<Expense>(),
            Income = new List<IncomeEntity>(),
            Receipts = new List<Receipt>()
        };
    }

    private void SetupPropertiesDbSet(List<Property> properties)
    {
        var mockDbSet = properties.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Properties).Returns(mockDbSet.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
