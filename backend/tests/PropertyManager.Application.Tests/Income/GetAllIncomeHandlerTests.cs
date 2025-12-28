using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Entities;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for GetAllIncomeHandler (AC-4.3.1, AC-4.3.2, AC-4.3.3, AC-4.3.4, AC-4.3.6).
/// </summary>
public class GetAllIncomeHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly GetAllIncomeHandler _handler;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPropertyId1 = Guid.NewGuid();
    private readonly Guid _testPropertyId2 = Guid.NewGuid();

    public GetAllIncomeHandlerTests()
    {
        _dbContextMock = new Mock<IAppDbContext>();
        _handler = new GetAllIncomeHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllIncome()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId2, "Property 2", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_DateFromFilter_ReturnsIncomeAfterDate()
    {
        // Arrange
        var dateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(dateFrom, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => DateOnly.Parse(i.Date.ToString("yyyy-MM-dd")) >= dateFrom);
    }

    [Fact]
    public async Task Handle_DateToFilter_ReturnsIncomeBeforeDate()
    {
        // Arrange
        var dateTo = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, dateTo, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => DateOnly.Parse(i.Date.ToString("yyyy-MM-dd")) <= dateTo);
    }

    [Fact]
    public async Task Handle_DateRangeFilter_ReturnsIncomeInRange()
    {
        // Arrange
        var dateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-12));
        var dateTo = DateOnly.FromDateTime(DateTime.Today.AddDays(-8));
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(dateFrom, dateTo, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Amount.Should().Be(1600.00m);
    }

    [Fact]
    public async Task Handle_PropertyFilter_ReturnsPropertyIncome()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId2, "Property 2", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, null, _testPropertyId1, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.PropertyId == _testPropertyId1);
    }

    [Fact]
    public async Task Handle_YearFilter_ReturnsIncomeForYear()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, new DateOnly(currentYear, 3, 15)),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1600.00m, new DateOnly(currentYear - 1, 6, 15)),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, new DateOnly(currentYear, 7, 20)),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, null, null, currentYear);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => DateOnly.Parse(i.Date.ToString("yyyy-MM-dd")).Year == currentYear);
    }

    [Fact]
    public async Task Handle_CombinedFilters_ReturnsCorrectIncome()
    {
        // Arrange
        var currentYear = DateTime.Today.Year;
        var dateFrom = new DateOnly(currentYear, 1, 1);
        var dateTo = new DateOnly(currentYear, 6, 30);
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, new DateOnly(currentYear, 3, 15)),
            CreateIncomeWithRelations(_testPropertyId2, "Property 2", 1600.00m, new DateOnly(currentYear, 4, 15)),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, new DateOnly(currentYear, 8, 20)),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1800.00m, new DateOnly(currentYear - 1, 3, 15)),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(dateFrom, dateTo, _testPropertyId1, currentYear);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Amount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task Handle_CalculatesTotalAmount_Correctly()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId2, "Property 2", 1600.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1700.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(4800.00m);
    }

    [Fact]
    public async Task Handle_OrdersByDateDescending()
    {
        // Arrange
        var incomeEntries = new List<IncomeEntity>
        {
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1000.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-30))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1500.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-5))),
            CreateIncomeWithRelations(_testPropertyId1, "Property 1", 1200.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-15))),
        };
        SetupIncomeDbSet(incomeEntries);

        var query = new GetAllIncomeQuery(null, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeInDescendingOrder(i => i.Date);
        result.Items[0].Amount.Should().Be(1500.00m); // Most recent
        result.Items[2].Amount.Should().Be(1000.00m); // Oldest
    }

    [Fact]
    public async Task Handle_NoIncome_ReturnsEmptyResult()
    {
        // Arrange
        SetupIncomeDbSet(new List<IncomeEntity>());

        var query = new GetAllIncomeQuery(null, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalAmount.Should().Be(0);
    }

    private IncomeEntity CreateIncomeWithRelations(Guid propertyId, string propertyName, decimal amount, DateOnly date, string? source = null, string? description = null)
    {
        var property = new Property
        {
            Id = propertyId,
            AccountId = _testAccountId,
            Name = propertyName,
            Street = "123 Test St",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        return new IncomeEntity
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            PropertyId = propertyId,
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

    private void SetupIncomeDbSet(List<IncomeEntity> incomeEntries)
    {
        var filteredIncome = incomeEntries
            .Where(i => i.DeletedAt == null)
            .ToList();

        var mockDbSet = filteredIncome.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Income).Returns(mockDbSet.Object);
    }
}
