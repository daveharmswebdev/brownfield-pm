using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for CreateIncomeCommandHandler (AC-4.1.3).
/// </summary>
public class CreateIncomeHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateIncomeCommandHandler _handler;
    private readonly List<IncomeEntity> _incomeEntries;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();

    public CreateIncomeHandlerTests()
    {
        _incomeEntries = new List<IncomeEntity>();

        var incomeDbSetMock = CreateMockDbSet(_incomeEntries);

        _dbContextMock = new Mock<IAppDbContext>();
        _dbContextMock.Setup(x => x.Income).Returns(incomeDbSetMock.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                foreach (var income in _incomeEntries.Where(i => i.Id == Guid.Empty))
                {
                    income.Id = Guid.NewGuid();
                }
            })
            .ReturnsAsync(1);

        // Setup property exists check
        var properties = new List<Property>
        {
            new()
            {
                Id = _testPropertyId,
                AccountId = _testAccountId,
                Name = "Test Property",
                Street = "123 Test St",
                City = "Austin",
                State = "TX",
                ZipCode = "78701"
            }
        };
        var propertiesDbSetMock = properties.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Properties).Returns(propertiesDbSetMock.Object);

        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new CreateIncomeCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: "January rent payment");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsAccountIdFromCurrentUser()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: "January rent payment");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _incomeEntries.Should().ContainSingle();
        _incomeEntries[0].AccountId.Should().Be(_testAccountId);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCreatedByUserId()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _incomeEntries.Should().ContainSingle();
        _incomeEntries[0].CreatedByUserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsAllIncomeFields()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: testDate,
            Source: "John Smith",
            Description: "January rent payment");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _incomeEntries.Should().ContainSingle();
        var income = _incomeEntries[0];
        income.PropertyId.Should().Be(_testPropertyId);
        income.Amount.Should().Be(1500.00m);
        income.Date.Should().Be(testDate);
        income.Source.Should().Be("John Smith");
        income.Description.Should().Be("January rent payment");
    }

    [Fact]
    public async Task Handle_ValidCommand_TrimsSourceAndDescription()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "  John Smith  ",
            Description: "  January rent payment  ");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _incomeEntries.Should().ContainSingle();
        var income = _incomeEntries[0];
        income.Source.Should().Be("John Smith");
        income.Description.Should().Be("January rent payment");
    }

    [Fact]
    public async Task Handle_ValidCommand_AllowsNullSourceAndDescription()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _incomeEntries.Should().ContainSingle();
        var income = _incomeEntries[0];
        income.Source.Should().BeNull();
        income.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PropertyNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentPropertyId = Guid.NewGuid();
        var command = new CreateIncomeCommand(
            PropertyId: nonExistentPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentPropertyId}*");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsIncomeToDbSet()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: _testPropertyId,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.Income.Add(It.IsAny<IncomeEntity>()), Times.Once);
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
