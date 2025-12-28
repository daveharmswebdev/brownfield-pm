using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for UpdatePropertyCommandHandler (AC-2.4.2, AC-2.4.5).
/// </summary>
public class UpdatePropertyHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdatePropertyCommandHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();

    public UpdatePropertyHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new UpdatePropertyCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAllPropertyFields()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Old Name", "Old Street", "Old City", "CA", "90210");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: "Updated Street",
            City: "Updated City",
            State: "TX",
            ZipCode: "78701");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.Name.Should().Be("Updated Name");
        property.Street.Should().Be("Updated Street");
        property.City.Should().Be("Updated City");
        property.State.Should().Be("TX");
        property.ZipCode.Should().Be("78701");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var originalUpdatedAt = property.UpdatedAt;
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: property.Street,
            City: property.City,
            State: property.State,
            ZipCode: property.ZipCode);

        // Act
        await Task.Delay(10); // Ensure time passes
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        property.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: property.Street,
            City: property.City,
            State: property.State,
            ZipCode: property.ZipCode);

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
        var command = new UpdatePropertyCommand(
            Id: nonExistentId,
            Name: "Test",
            Street: "Test Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_PropertyBelongsToDifferentAccount_ThrowsNotFoundException()
    {
        // Arrange
        var otherAccountProperty = CreateProperty(_otherAccountId, "Other Property", "Other Street", "Houston", "TX", "77001");
        var properties = new List<Property> { otherAccountProperty };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: otherAccountProperty.Id,
            Name: "Attempt to Update",
            Street: "Hacker Street",
            City: "Hackerville",
            State: "TX",
            ZipCode: "12345");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedProperty_ThrowsNotFoundException()
    {
        // Arrange
        var deletedProperty = CreateProperty(_testAccountId, "Deleted Property", "Deleted Street", "Dallas", "TX", "75201");
        deletedProperty.DeletedAt = DateTime.UtcNow;
        var properties = new List<Property> { deletedProperty };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: deletedProperty.Id,
            Name: "Attempt to Update Deleted",
            Street: "Test Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LowercaseState_ConvertsToUppercase()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: property.Name,
            Street: property.Street,
            City: property.City,
            State: "ca", // lowercase
            ZipCode: property.ZipCode);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.State.Should().Be("CA");
    }

    [Fact]
    public async Task Handle_DoesNotChangeAccountId()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: "Updated Street",
            City: "Updated City",
            State: "CA",
            ZipCode: "90210");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.AccountId.Should().Be(_testAccountId);
    }

    [Fact]
    public async Task Handle_DoesNotChangeCreatedAt()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var originalCreatedAt = property.CreatedAt;
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: property.Street,
            City: property.City,
            State: property.State,
            ZipCode: property.ZipCode);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task Handle_DoesNotChangePropertyId()
    {
        // Arrange
        var property = CreateProperty(_testAccountId, "Test Property", "Test Street", "Austin", "TX", "78701");
        var originalId = property.Id;
        var properties = new List<Property> { property };
        SetupPropertiesDbSet(properties);

        var command = new UpdatePropertyCommand(
            Id: property.Id,
            Name: "Updated Name",
            Street: property.Street,
            City: property.City,
            State: property.State,
            ZipCode: property.ZipCode);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        property.Id.Should().Be(originalId);
    }

    private Property CreateProperty(Guid accountId, string name, string street, string city, string state, string zipCode)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name,
            Street = street,
            City = city,
            State = state,
            ZipCode = zipCode,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
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
