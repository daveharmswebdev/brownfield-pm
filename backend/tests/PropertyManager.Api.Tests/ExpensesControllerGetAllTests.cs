using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Application.Expenses;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for GET /api/v1/expenses endpoint (AC-3.4.1, AC-3.4.3, AC-3.4.4, AC-3.4.5, AC-3.4.8).
/// </summary>
public class ExpensesControllerGetAllTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExpensesControllerGetAllTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================================================
    // GET /api/v1/expenses Tests (AC-3.4.1, AC-3.4.8)
    // =====================================================

    [Fact]
    public async Task GetAllExpenses_WithoutAuth_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/expenses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllExpenses_NoExpenses_ReturnsEmptyList()
    {
        // Arrange (AC-3.4.7)
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().BeEmpty();
        content.TotalCount.Should().Be(0);
        content.Page.Should().Be(1);
        content.PageSize.Should().Be(50);
        content.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetAllExpenses_WithExpenses_ReturnsPaginated()
    {
        // Arrange (AC-3.4.1, AC-3.4.8)
        var email = $"getall-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create 3 expenses
        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Expense One");
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "Expense Two");
        await CreateExpenseAsync(propertyId, accessToken, 300.00m, "Expense Three");

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(3);
        content.TotalCount.Should().Be(3);
        content.Page.Should().Be(1);
        content.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetAllExpenses_IncludesPropertyName()
    {
        // Arrange (AC-3.4.2)
        var email = $"property-name-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken, "My Test Property");
        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Test Expense");

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].PropertyName.Should().Be("My Test Property");
    }

    [Fact]
    public async Task GetAllExpenses_SortedByDateDescending()
    {
        // Arrange (AC-3.4.1)
        var email = $"sorted-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create expenses with different dates
        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Oldest", DateOnly.FromDateTime(DateTime.Today.AddDays(-30)));
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "Newest", DateOnly.FromDateTime(DateTime.Today));
        await CreateExpenseAsync(propertyId, accessToken, 150.00m, "Middle", DateOnly.FromDateTime(DateTime.Today.AddDays(-15)));

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(3);
        content.Items[0].Description.Should().Be("Newest");
        content.Items[1].Description.Should().Be("Middle");
        content.Items[2].Description.Should().Be("Oldest");
    }

    [Fact]
    public async Task GetAllExpenses_WithPageSize_RespectsPageSize()
    {
        // Arrange (AC-3.4.8)
        var email = $"pagesize-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create 5 expenses
        for (int i = 0; i < 5; i++)
        {
            await CreateExpenseAsync(propertyId, accessToken, 100.00m + i, $"Expense {i}");
        }

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses?pageSize=2", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(2);
        content.TotalCount.Should().Be(5);
        content.PageSize.Should().Be(2);
        content.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetAllExpenses_WithPage_ReturnsCorrectPage()
    {
        // Arrange (AC-3.4.8)
        var email = $"pagination-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create 5 expenses
        for (int i = 0; i < 5; i++)
        {
            await CreateExpenseAsync(propertyId, accessToken, 100.00m + i, $"Expense {i}");
        }

        // Act - Get page 2
        var response = await GetWithAuthAsync("/api/v1/expenses?pageSize=2&page=2", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(2);
        content.Page.Should().Be(2);
    }

    // =====================================================
    // Filter Tests (AC-3.4.3, AC-3.4.4, AC-3.4.5)
    // =====================================================

    [Fact]
    public async Task GetAllExpenses_WithYearFilter_ReturnsMatchingYear()
    {
        // Arrange (AC-3.4.1)
        var email = $"year-filter-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "2025 Expense", new DateOnly(2025, 6, 15));
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "2024 Expense", new DateOnly(2024, 6, 15));

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses?year=2025", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("2025 Expense");
    }

    [Fact]
    public async Task GetAllExpenses_WithDateRange_ReturnsMatchingDates()
    {
        // Arrange (AC-3.4.3) - use past dates to pass validation (expense dates cannot be in future)
        var email = $"date-range-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Before", new DateOnly(2024, 1, 1));
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "In Range", new DateOnly(2024, 6, 15));
        await CreateExpenseAsync(propertyId, accessToken, 300.00m, "After", new DateOnly(2024, 11, 30));

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses?dateFrom=2024-06-01&dateTo=2024-06-30", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("In Range");
    }

    [Fact]
    public async Task GetAllExpenses_WithCategoryFilter_ReturnsMatchingCategory()
    {
        // Arrange (AC-3.4.4)
        var email = $"category-filter-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Get categories
        var categoriesResponse = await GetWithAuthAsync("/api/v1/expense-categories", accessToken);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<ExpenseCategoriesResponse>();
        var repairsCategory = categories!.Items.First(c => c.Name == "Repairs");
        var utilitiesCategory = categories!.Items.First(c => c.Name == "Utilities");

        // Create expenses with different categories
        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Repairs Expense", categoryId: repairsCategory.Id);
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "Utilities Expense", categoryId: utilitiesCategory.Id);

        // Act - Filter by Repairs category
        var response = await GetWithAuthAsync($"/api/v1/expenses?categoryIds={repairsCategory.Id}", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("Repairs Expense");
    }

    [Fact]
    public async Task GetAllExpenses_WithSearchFilter_ReturnsMatchingDescription()
    {
        // Arrange (AC-3.4.5)
        var email = $"search-filter-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Plumbing repair in bathroom");
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "Electrical work");

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses?search=plumbing", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Contain("Plumbing");
    }

    [Fact]
    public async Task GetAllExpenses_SearchFilter_CaseInsensitive()
    {
        // Arrange (AC-3.4.5)
        var email = $"search-case-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Plumbing repair");

        // Act - Search with uppercase
        var response = await GetWithAuthAsync("/api/v1/expenses?search=PLUMBING", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllExpenses_MultipleFilters_ReturnsIntersection()
    {
        // Arrange
        var email = $"multi-filter-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Plumbing Q1", new DateOnly(2025, 2, 15));
        await CreateExpenseAsync(propertyId, accessToken, 200.00m, "Plumbing Q4", new DateOnly(2025, 11, 15));
        await CreateExpenseAsync(propertyId, accessToken, 300.00m, "Electrical Q1", new DateOnly(2025, 3, 15));

        // Act - Search for "plumbing" in Q1
        var response = await GetWithAuthAsync(
            "/api/v1/expenses?search=plumbing&dateFrom=2025-01-01&dateTo=2025-03-31",
            accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("Plumbing Q1");
    }

    [Fact]
    public async Task GetAllExpenses_NoMatches_ReturnsEmptyList()
    {
        // Arrange (AC-3.4.7)
        var email = $"no-matches-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Plumbing repair");

        // Act - Search for non-existent term
        var response = await GetWithAuthAsync("/api/v1/expenses?search=nonexistent", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().BeEmpty();
        content.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllExpenses_MultipleProperties_ReturnsAll()
    {
        // Arrange (AC-3.4.1 - across all properties)
        var email = $"multi-property-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var property1 = await CreatePropertyAsync(accessToken, "Property One");
        var property2 = await CreatePropertyAsync(accessToken, "Property Two");

        await CreateExpenseAsync(property1, accessToken, 100.00m, "Expense Property 1");
        await CreateExpenseAsync(property2, accessToken, 200.00m, "Expense Property 2");

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().HaveCount(2);
        content.Items.Select(i => i.PropertyName).Should().Contain("Property One");
        content.Items.Select(i => i.PropertyName).Should().Contain("Property Two");
    }

    [Fact]
    public async Task GetAllExpenses_OtherUserExpenses_NotVisible()
    {
        // Arrange - Account isolation
        var email1 = $"user1-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        var property1 = await CreatePropertyAsync(accessToken1);
        await CreateExpenseAsync(property1, accessToken1, 100.00m, "User 1 Expense");

        // Act - User 2 tries to see User 1's expenses
        var response = await GetWithAuthAsync("/api/v1/expenses", accessToken2);

        // Assert - User 2 sees empty list (their expenses only)
        var content = await response.Content.ReadFromJsonAsync<PagedExpenseListResponse>();
        content!.Items.Should().BeEmpty();
    }

    // =====================================================
    // Helper Methods
    // =====================================================

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        return accessToken;
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginAsync(string email)
    {
        var password = "Test@123456";

        var registerRequest = new
        {
            Email = email,
            Password = password,
            Name = "Test Account"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var fakeEmailService = scope.ServiceProvider.GetRequiredService<FakeEmailService>();
        var verificationToken = fakeEmailService.SentVerificationEmails.Last().Token;

        var verifyRequest = new { Token = verificationToken };
        var verifyResponse = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyRequest);
        verifyResponse.EnsureSuccessStatusCode();

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return (loginContent!.AccessToken, Guid.Empty);
    }

    private async Task<Guid> CreatePropertyAsync(string accessToken, string name = "Test Property")
    {
        var createRequest = new
        {
            Name = name,
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>();
        return content!.Id;
    }

    private async Task<Guid> CreateExpenseAsync(
        Guid propertyId,
        string accessToken,
        decimal amount = 100.00m,
        string description = "Test Expense",
        DateOnly? date = null,
        Guid? categoryId = null)
    {
        if (categoryId == null)
        {
            var categoriesResponse = await GetWithAuthAsync("/api/v1/expense-categories", accessToken);
            categoriesResponse.EnsureSuccessStatusCode();
            var categories = await categoriesResponse.Content.ReadFromJsonAsync<ExpenseCategoriesResponse>();
            categoryId = categories!.Items[0].Id;
        }

        var createRequest = new
        {
            PropertyId = propertyId,
            Amount = amount,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            CategoryId = categoryId.Value,
            Description = description
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/expenses", createRequest, accessToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CreateExpenseResponse>();
        return content!.Id;
    }

    private async Task<HttpResponseMessage> PostAsJsonWithAuthAsync<T>(string url, T content, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(content);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetWithAuthAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return await _client.SendAsync(request);
    }
}

// Response records for deserialization
public record PagedExpenseListResponse(
    List<ExpenseListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
