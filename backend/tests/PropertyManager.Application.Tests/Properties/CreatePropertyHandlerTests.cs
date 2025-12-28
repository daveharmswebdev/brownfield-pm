using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for CreatePropertyCommandHandler (AC-2.1.3, AC-2.1.4).
/// </summary>
public class CreatePropertyHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreatePropertyCommandHandler _handler;
    private readonly List<Property> _properties;
    private readonly Guid _testAccountId = Guid.NewGuid();

    public CreatePropertyHandlerTests()
    {
        _properties = new List<Property>();

        var propertiesDbSetMock = CreateMockDbSet(_properties);

        _dbContextMock = new Mock<IAppDbContext>();
        _dbContextMock.Setup(x => x.Properties).Returns(propertiesDbSetMock.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                // Simulate DB generating Id
                foreach (var property in _properties.Where(p => p.Id == Guid.Empty))
                {
                    property.Id = Guid.NewGuid();
                }
            })
            .ReturnsAsync(1);

        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new CreatePropertyCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsAccountIdFromCurrentUser()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _properties.Should().ContainSingle();
        _properties[0].AccountId.Should().Be(_testAccountId);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsAllPropertyFields()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "tx", // lowercase to test normalization
            ZipCode: "78701");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _properties.Should().ContainSingle();
        var property = _properties[0];
        property.Name.Should().Be("Oak Street Duplex");
        property.Street.Should().Be("123 Oak Street");
        property.City.Should().Be("Austin");
        property.State.Should().Be("TX"); // Should be uppercase
        property.ZipCode.Should().Be("78701");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsPropertyToDbSet()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.Properties.Add(It.IsAny<Property>()), Times.Once);
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);

        return mockSet;
    }
}
