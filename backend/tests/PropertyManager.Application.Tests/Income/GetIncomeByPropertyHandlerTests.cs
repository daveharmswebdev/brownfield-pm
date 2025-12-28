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
/// Unit tests for GetIncomeByPropertyQueryHandler (AC-4.1.2, AC-4.1.6).
/// </summary>
public class GetIncomeByPropertyHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly GetIncomeByPropertyQueryHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPropertyId = Guid.NewGuid();

    public GetIncomeByPropertyHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new GetIncomeByPropertyQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProperty_ReturnsIncomeList()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-35))),
        };
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(incomeEntries);

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ValidProperty_ReturnsYtdTotal()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-35))),
        };
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(incomeEntries);

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.YtdTotal.Should().Be(3100.00m);
    }

    [Fact]
    public async Task Handle_ValidProperty_ReturnsIncomeOrderedByDateDescending()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(1000.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-30))),
            CreateIncomeWithRelations(1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(1200.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(incomeEntries);

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeInDescendingOrder(i => i.Date);
        result.Items[0].Amount.Should().Be(1500.00m); // Most recent
        result.Items[2].Amount.Should().Be(1000.00m); // Oldest
    }

    [Fact]
    public async Task Handle_WithYearFilter_ReturnsOnlyFilteredYear()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(1500.00m, new DateOnly(currentYear, 3, 15)),
            CreateIncomeWithRelations(1600.00m, new DateOnly(currentYear - 1, 6, 15)),
        };
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(incomeEntries);

        var query = new GetIncomeByPropertyQuery(_testPropertyId, currentYear);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Amount.Should().Be(1500.00m);
        result.YtdTotal.Should().Be(1500.00m);
    }

    [Fact]
    public async Task Handle_NoIncome_ReturnsEmptyList()
    {
        // Arrange
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(new List<IncomeEntity>());

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.YtdTotal.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PropertyNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentPropertyId = Guid.NewGuid();
        SetupPropertiesDbSet(nonExistentPropertyId, false);
        SetupIncomeDbSet(new List<IncomeEntity>());

        var query = new GetIncomeByPropertyQuery(nonExistentPropertyId, null);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentPropertyId}*");
    }

    [Fact]
    public async Task Handle_ValidProperty_ReturnsPropertyName()
    {
        // Arrange
        var income = CreateIncomeWithRelations(1500.00m, DateOnly.FromDateTime(DateTime.Today));
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].PropertyName.Should().Be("Test Property");
    }

    [Fact]
    public async Task Handle_ValidProperty_ReturnsIncomeDetails()
    {
        // Arrange
        var income = CreateIncomeWithRelations(1500.00m, DateOnly.FromDateTime(DateTime.Today), "John Smith", "January rent");
        SetupPropertiesDbSet(_testPropertyId, true);
        SetupIncomeDbSet(new List<IncomeEntity> { income });

        var query = new GetIncomeByPropertyQuery(_testPropertyId, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var item = result.Items.Should().ContainSingle().Subject;
        item.Source.Should().Be("John Smith");
        item.Description.Should().Be("January rent");
    }

    private IncomeEntity CreateIncomeWithRelations(decimal amount, DateOnly date, string? source = null, string? description = null)
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

        return new IncomeEntity
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = _testPropertyId,
            Property = property,
            Amount = amount,
            Date = date,
            Source = source,
            Description = description,
            CreatedByUserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    private void SetupPropertiesDbSet(Guid propertyId, bool exists)
    {
        var properties = exists
            ? new List<Property>
            {
                new()
                {
                    Id = propertyId,
                    AccountId = _testAccountId,
                    Name = "Test Property",
                    Street = "123 Test St",
                    City = "Austin",
                    State = "TX",
                    ZipCode = "78701"
                }
            }
            : new List<Property>();

        var mockDbSet = properties.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Properties).Returns(mockDbSet.Object);
    }

    private void SetupIncomeDbSet(List<IncomeEntity> incomeEntries)
    {
        var filteredIncome = incomeEntries
            .Where(i => i.DeletedAt == null && i.PropertyId == _testPropertyId)
            .ToList();

        var mockDbSet = filteredIncome.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Income).Returns(mockDbSet.Object);
    }
}
