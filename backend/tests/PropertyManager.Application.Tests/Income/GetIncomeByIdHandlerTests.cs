using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Entities;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for GetIncomeByIdQueryHandler (AC-4.2.2).
/// </summary>
public class GetIncomeByIdHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly GetIncomeByIdQueryHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherAccountId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();

    public GetIncomeByIdHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new GetIncomeByIdQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsIncomeDto()
    {
        // Arrange
        var income = CreateIncomeWithProperty(_testAccountId, 1500.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var query = new GetIncomeByIdQuery(income.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(income.Id);
        result.Amount.Should().Be(1500.00m);
        result.PropertyId.Should().Be(_testPropertyId);
        result.PropertyName.Should().Be("Test Property");
    }

    [Fact]
    public async Task Handle_ValidId_ReturnsAllFields()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var income = CreateIncomeWithProperty(_testAccountId, 2500.00m, testDate);
        income.Source = "John Smith - Rent";
        income.Description = "January rent payment";
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var query = new GetIncomeByIdQuery(income.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Id.Should().Be(income.Id);
        result.PropertyId.Should().Be(_testPropertyId);
        result.PropertyName.Should().Be("Test Property");
        result.Amount.Should().Be(2500.00m);
        result.Date.Should().Be(testDate);
        result.Source.Should().Be("John Smith - Rent");
        result.Description.Should().Be("January rent payment");
        result.CreatedAt.Should().BeCloseTo(income.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_IncomeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        SetupIncomeDbSet(new List<IncomeEntity>());

        var nonExistentId = Guid.NewGuid();
        var query = new GetIncomeByIdQuery(nonExistentId);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task Handle_WrongAccount_ThrowsNotFoundException()
    {
        // Arrange - income belongs to different account (simulated by global query filter)
        var otherAccountIncome = CreateIncomeWithProperty(_otherAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        // Don't add to mock - simulates global query filter excluding it
        SetupIncomeDbSet(new List<IncomeEntity>());

        var query = new GetIncomeByIdQuery(otherAccountIncome.Id);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedIncome_ThrowsNotFoundException()
    {
        // Arrange
        var deletedIncome = CreateIncomeWithProperty(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        deletedIncome.DeletedAt = DateTime.UtcNow.AddDays(-1);
        SetupIncomeDbSet(new List<IncomeEntity> { deletedIncome });

        var query = new GetIncomeByIdQuery(deletedIncome.Id);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullSourceAndDescription_ReturnsNullValues()
    {
        // Arrange
        var income = CreateIncomeWithProperty(_testAccountId, 1000.00m, DateOnly.FromDateTime(DateTime.Today));
        income.Source = null;
        income.Description = null;
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var query = new GetIncomeByIdQuery(income.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Source.Should().BeNull();
        result.Description.Should().BeNull();
    }

    private IncomeEntity CreateIncomeWithProperty(Guid accountId, decimal amount, DateOnly date)
    {
        var property = new Property
        {
            Id = _testPropertyId,
            AccountId = accountId,
            Name = "Test Property",
            Street = "123 Test St",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        return new IncomeEntity
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PropertyId = _testPropertyId,
            Property = property,
            Amount = amount,
            Date = date,
            Source = "Test source",
            Description = "Test description",
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
    }
}
