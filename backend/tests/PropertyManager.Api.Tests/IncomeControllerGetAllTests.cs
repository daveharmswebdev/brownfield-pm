using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Application.Income;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for GET /api/v1/income endpoint (AC-4.3.1, AC-4.3.2, AC-4.3.3, AC-4.3.4, AC-4.3.6).
/// </summary>
public class IncomeControllerGetAllTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IncomeControllerGetAllTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================================================
    // GET /api/v1/income Tests (AC-4.3.1)
    // =====================================================

    [Fact]
    public async Task GetAllIncome_WithoutAuth_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/income");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllIncome_NoIncome_ReturnsEmptyList()
    {
        // Arrange (AC-4.3.5)
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().BeEmpty();
        content.TotalCount.Should().Be(0);
        content.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllIncome_WithIncome_ReturnsAllIncome()
    {
        // Arrange (AC-4.3.1)
        var email = $"getall-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create 3 income entries
        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Rent January");
        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Rent February");
        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Rent March");

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(3);
        content.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllIncome_CalculatesTotalAmount()
    {
        // Arrange (AC-4.3.6)
        var email = $"total-amount-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Rent 1");
        await CreateIncomeAsync(propertyId, accessToken, 1600.00m, "Rent 2");
        await CreateIncomeAsync(propertyId, accessToken, 1700.00m, "Rent 3");

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.TotalAmount.Should().Be(4800.00m);
    }

    [Fact]
    public async Task GetAllIncome_IncludesPropertyName()
    {
        // Arrange (AC-4.3.2)
        var email = $"property-name-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken, "My Rental Property");
        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Rent");

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].PropertyName.Should().Be("My Rental Property");
    }

    [Fact]
    public async Task GetAllIncome_SortedByDateDescending()
    {
        // Arrange (AC-4.3.2)
        var email = $"sorted-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create income with different dates
        await CreateIncomeAsync(propertyId, accessToken, 1000.00m, "Oldest", DateOnly.FromDateTime(DateTime.Today.AddDays(-30)));
        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Newest", DateOnly.FromDateTime(DateTime.Today));
        await CreateIncomeAsync(propertyId, accessToken, 1200.00m, "Middle", DateOnly.FromDateTime(DateTime.Today.AddDays(-15)));

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(3);
        content.Items[0].Description.Should().Be("Newest");
        content.Items[1].Description.Should().Be("Middle");
        content.Items[2].Description.Should().Be("Oldest");
    }

    // =====================================================
    // Filter Tests (AC-4.3.3, AC-4.3.4)
    // =====================================================

    [Fact]
    public async Task GetAllIncome_WithYearFilter_ReturnsMatchingYear()
    {
        // Arrange (AC-4.3.2)
        var email = $"year-filter-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "2025 Income", new DateOnly(2025, 6, 15));
        await CreateIncomeAsync(propertyId, accessToken, 1600.00m, "2024 Income", new DateOnly(2024, 6, 15));

        // Act
        var response = await GetWithAuthAsync("/api/v1/income?year=2025", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("2025 Income");
    }

    [Fact]
    public async Task GetAllIncome_WithDateRange_ReturnsMatchingDates()
    {
        // Arrange (AC-4.3.3)
        var email = $"date-range-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Before", new DateOnly(2024, 1, 1));
        await CreateIncomeAsync(propertyId, accessToken, 1600.00m, "In Range", new DateOnly(2024, 6, 15));
        await CreateIncomeAsync(propertyId, accessToken, 1700.00m, "After", new DateOnly(2024, 11, 30));

        // Act
        var response = await GetWithAuthAsync("/api/v1/income?dateFrom=2024-06-01&dateTo=2024-06-30", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("In Range");
    }

    [Fact]
    public async Task GetAllIncome_WithPropertyFilter_ReturnsPropertyIncome()
    {
        // Arrange (AC-4.3.4)
        var email = $"property-filter-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var property1 = await CreatePropertyAsync(accessToken, "Property One");
        var property2 = await CreatePropertyAsync(accessToken, "Property Two");

        await CreateIncomeAsync(property1, accessToken, 1500.00m, "Income Property 1");
        await CreateIncomeAsync(property2, accessToken, 1600.00m, "Income Property 2");
        await CreateIncomeAsync(property1, accessToken, 1700.00m, "Income Property 1 Again");

        // Act - Filter by property 1
        var response = await GetWithAuthAsync($"/api/v1/income?propertyId={property1}", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(2);
        content.Items.Should().OnlyContain(i => i.PropertyName == "Property One");
    }

    [Fact]
    public async Task GetAllIncome_MultipleFilters_ReturnsIntersection()
    {
        // Arrange
        var email = $"multi-filter-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var property1 = await CreatePropertyAsync(accessToken, "Property One");
        var property2 = await CreatePropertyAsync(accessToken, "Property Two");

        await CreateIncomeAsync(property1, accessToken, 1500.00m, "P1 Q1", new DateOnly(2025, 2, 15));
        await CreateIncomeAsync(property1, accessToken, 1600.00m, "P1 Q4", new DateOnly(2025, 11, 15));
        await CreateIncomeAsync(property2, accessToken, 1700.00m, "P2 Q1", new DateOnly(2025, 3, 15));

        // Act - Filter by property 1 in Q1
        var response = await GetWithAuthAsync(
            $"/api/v1/income?propertyId={property1}&dateFrom=2025-01-01&dateTo=2025-03-31",
            accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Description.Should().Be("P1 Q1");
    }

    [Fact]
    public async Task GetAllIncome_NoMatches_ReturnsEmptyList()
    {
        // Arrange (AC-4.3.5)
        var email = $"no-matches-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "Income 2024", new DateOnly(2024, 6, 15));

        // Act - Filter for 2025
        var response = await GetWithAuthAsync("/api/v1/income?year=2025", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().BeEmpty();
        content.TotalCount.Should().Be(0);
        content.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllIncome_MultipleProperties_ReturnsAll()
    {
        // Arrange (AC-4.3.1 - across all properties)
        var email = $"multi-property-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var property1 = await CreatePropertyAsync(accessToken, "Property One");
        var property2 = await CreatePropertyAsync(accessToken, "Property Two");

        await CreateIncomeAsync(property1, accessToken, 1500.00m, "Income Property 1");
        await CreateIncomeAsync(property2, accessToken, 1600.00m, "Income Property 2");

        // Act
        var response = await GetWithAuthAsync("/api/v1/income", accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(2);
        content.Items.Select(i => i.PropertyName).Should().Contain("Property One");
        content.Items.Select(i => i.PropertyName).Should().Contain("Property Two");
    }

    [Fact]
    public async Task GetAllIncome_OtherUserIncome_NotVisible()
    {
        // Arrange - Account isolation
        var email1 = $"user1-income-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-income-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        var property1 = await CreatePropertyAsync(accessToken1);
        await CreateIncomeAsync(property1, accessToken1, 1500.00m, "User 1 Income");

        // Act - User 2 tries to see User 1's income
        var response = await GetWithAuthAsync("/api/v1/income", accessToken2);

        // Assert - User 2 sees empty list (their income only)
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllIncome_TotalReflectsFilters()
    {
        // Arrange (AC-4.3.6 - total reflects filtered results)
        var email = $"total-filtered-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        await CreateIncomeAsync(propertyId, accessToken, 1500.00m, "2025 Income", new DateOnly(2025, 6, 15));
        await CreateIncomeAsync(propertyId, accessToken, 1600.00m, "2024 Income", new DateOnly(2024, 6, 15));

        // Act - Filter for 2025 only
        var response = await GetWithAuthAsync("/api/v1/income?year=2025", accessToken);

        // Assert - Total should only include 2025 income
        var content = await response.Content.ReadFromJsonAsync<IncomeListResponse>();
        content!.Items.Should().HaveCount(1);
        content.TotalCount.Should().Be(1);
        content.TotalAmount.Should().Be(1500.00m);
    }

    // =====================================================
    // Helper Methods
    // =====================================================

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"test-income-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        return accessToken;
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginAsync(string email)
    {
        var password = "Test@123456";

        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, email);

        var registerRequest = new { Password = password, Token = invitationToken };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return (loginContent!.AccessToken, Guid.Empty);
    }

    private async Task<(string email, string password, Guid accountId)> CreateOwnerUser()
    {
        var email = $"owner{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = new Account { Name = email.Split('@')[0] };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            AccountId = account.Id,
            Role = "Owner"
        };

        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return (email, password, account.Id);
    }

    private async Task<string> SendInvitationAndGetToken(string ownerEmail, string ownerPassword, string inviteeEmail)
    {
        var accessToken = await LoginAndGetTokenInternal(ownerEmail, ownerPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/invite");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(new { Email = inviteeEmail });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var fakeEmailService = scope.ServiceProvider.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentInvitationEmails.FirstOrDefault(e => e.Email == inviteeEmail);
        sentEmail.Should().NotBeNull($"Invitation email should have been sent to {inviteeEmail}");

        return sentEmail!.Token;
    }

    private async Task<string> LoginAndGetTokenInternal(string email, string password)
    {
        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
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

    private async Task<Guid> CreateIncomeAsync(
        Guid propertyId,
        string accessToken,
        decimal amount = 1500.00m,
        string? description = null,
        DateOnly? date = null)
    {
        var createRequest = new
        {
            PropertyId = propertyId,
            Amount = amount,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Source = "Tenant",
            Description = description
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/income", createRequest, accessToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CreateIncomeResponse>();
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
public record IncomeListResponse(
    List<IncomeDto> Items,
    int TotalCount,
    decimal TotalAmount
);

public record CreateIncomeResponse(Guid Id);
