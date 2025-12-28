using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for DashboardController (AC-4.4.1, AC-4.4.2, AC-4.4.6).
/// </summary>
public class DashboardControllerTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardControllerTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTotals_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/dashboard/totals?year=2025");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTotals_NoData_ReturnsZeros()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.TotalExpenses.Should().Be(0);
        content.TotalIncome.Should().Be(0);
        content.NetIncome.Should().Be(0);
        content.PropertyCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTotals_WithProperties_ReturnsPropertyCount()
    {
        // Arrange
        var email = $"dashboard-count-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create two properties
        await CreatePropertyAsync(accessToken, "Property 1");
        await CreatePropertyAsync(accessToken, "Property 2");

        // Act
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.PropertyCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTotals_WithExpensesAndIncome_CalculatesNetCorrectly()
    {
        // Arrange
        var email = $"dashboard-net-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var propertyId = await CreatePropertyAsync(accessToken, "Net Test Property");

        // Create expenses (total: $500)
        await CreateExpenseAsync(accessToken, propertyId, 200m, "2025-01-15");
        await CreateExpenseAsync(accessToken, propertyId, 300m, "2025-02-15");

        // Create income (total: $1000)
        await CreateIncomeAsync(accessToken, propertyId, 600m, "2025-01-01");
        await CreateIncomeAsync(accessToken, propertyId, 400m, "2025-02-01");

        // Act
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.TotalExpenses.Should().Be(500m);
        content.TotalIncome.Should().Be(1000m);
        content.NetIncome.Should().Be(500m); // Income - Expenses
    }

    [Fact]
    public async Task GetTotals_WithYearFilter_FiltersCorrectly()
    {
        // Arrange
        var email = $"dashboard-year-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken, "Year Filter Property");

        // Create expenses in 2024
        await CreateExpenseAsync(accessToken, propertyId, 100m, "2024-06-15");

        // Create expenses in 2025
        await CreateExpenseAsync(accessToken, propertyId, 200m, "2025-06-15");

        // Create income in 2024
        await CreateIncomeAsync(accessToken, propertyId, 500m, "2024-06-01");

        // Create income in 2025
        await CreateIncomeAsync(accessToken, propertyId, 300m, "2025-06-01");

        // Act - Get 2025 totals
        var response2025 = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert - 2025 should only have 2025 data
        response2025.StatusCode.Should().Be(HttpStatusCode.OK);

        var content2025 = await response2025.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content2025.Should().NotBeNull();
        content2025!.TotalExpenses.Should().Be(200m);
        content2025.TotalIncome.Should().Be(300m);
        content2025.NetIncome.Should().Be(100m);

        // Act - Get 2024 totals
        var response2024 = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2024", accessToken);

        // Assert - 2024 should only have 2024 data
        var content2024 = await response2024.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content2024.Should().NotBeNull();
        content2024!.TotalExpenses.Should().Be(100m);
        content2024.TotalIncome.Should().Be(500m);
        content2024.NetIncome.Should().Be(400m);
    }

    [Fact]
    public async Task GetTotals_AccountIsolation_OnlyReturnsOwnData()
    {
        // Arrange - Create data with two different users
        var email1 = $"user1-dash-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-dash-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates property with income
        var property1 = await CreatePropertyAsync(accessToken1, "User1 Property");
        await CreateIncomeAsync(accessToken1, property1, 1000m, "2025-03-01");

        // User 2 creates property with income
        var property2 = await CreatePropertyAsync(accessToken2, "User2 Property");
        await CreateIncomeAsync(accessToken2, property2, 5000m, "2025-03-01");

        // Act - User 1 gets their dashboard
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken1);

        // Assert - Should only see User 1's data
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.TotalIncome.Should().Be(1000m);
        content.PropertyCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTotals_ExcludesSoftDeletedProperties_FromPropertyCount()
    {
        // Arrange - Historical income data is preserved for tax purposes even when property is deleted
        // But property count should only count active properties
        var email = $"dashboard-softdelete-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create two properties
        var property1 = await CreatePropertyAsync(accessToken, "Keep Property");
        var property2 = await CreatePropertyAsync(accessToken, "Delete Property");

        // Add income to both
        await CreateIncomeAsync(accessToken, property1, 1000m, "2025-04-01");
        await CreateIncomeAsync(accessToken, property2, 2000m, "2025-04-01");

        // Delete property 2
        await DeletePropertyAsync(accessToken, property2);

        // Act
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert - Property count should exclude deleted property
        // But income from deleted property is still counted (historical data preserved)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.PropertyCount.Should().Be(1); // Only active property
        content.TotalIncome.Should().Be(3000m); // Both properties' income still counted
    }

    [Fact]
    public async Task GetTotals_NegativeNet_CalculatesCorrectly()
    {
        // Arrange (AC-4.4.2 - Net can be negative)
        var email = $"dashboard-negative-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken, "Negative Net Property");

        // Create more expenses than income
        await CreateExpenseAsync(accessToken, propertyId, 5000m, "2025-05-15");
        await CreateIncomeAsync(accessToken, propertyId, 2000m, "2025-05-01");

        // Act
        var response = await GetWithAuthAsync("/api/v1/dashboard/totals?year=2025", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<DashboardTotalsDto>();
        content.Should().NotBeNull();
        content!.TotalExpenses.Should().Be(5000m);
        content.TotalIncome.Should().Be(2000m);
        content.NetIncome.Should().Be(-3000m); // Negative net income
    }

    #region Helper Methods

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        return accessToken;
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginAsync(string email)
    {
        var password = "Test@123456";

        // Create an owner user to send the invitation
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();

        // Login as owner and send invitation
        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, email);

        // Register with the invitation token
        var registerRequest = new { Password = password, Token = invitationToken };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        // Login as the newly registered user
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

        // Create account
        var account = new Account { Name = email.Split('@')[0] };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        // Create user with email confirmed
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

        // Get the invitation token from the fake email service
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

    private async Task<Guid> CreatePropertyAsync(string accessToken, string name)
    {
        var request = new
        {
            Name = name,
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>();
        return content!.Id;
    }

    private async Task CreateExpenseAsync(string accessToken, Guid propertyId, decimal amount, string dateString)
    {
        // Get a valid category ID first
        var categoriesResponse = await GetWithAuthAsync("/api/v1/expense-categories", accessToken);
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<ExpenseCategoriesResponse>();
        var categoryId = categories!.Items[0].Id;

        var date = DateOnly.Parse(dateString);
        var request = new
        {
            PropertyId = propertyId,
            Amount = amount,
            Date = date,
            CategoryId = categoryId,
            Description = "Test expense"
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/expenses", request, accessToken);
        response.EnsureSuccessStatusCode();
    }

    private record ExpenseCategoriesResponse(IReadOnlyList<ExpenseCategoryDto> Items);
    private record ExpenseCategoryDto(Guid Id, string Name);

    private async Task CreateIncomeAsync(string accessToken, Guid propertyId, decimal amount, string dateString)
    {
        var date = DateOnly.Parse(dateString);
        var request = new
        {
            PropertyId = propertyId,
            Amount = amount,
            Date = date,
            Source = "Rent",
            Description = "Test income"
        };
        var response = await PostAsJsonWithAuthAsync("/api/v1/income", request, accessToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeletePropertyAsync(string accessToken, Guid propertyId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/properties/{propertyId}");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
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

    #endregion
}

public record DashboardTotalsDto(
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal NetIncome,
    int PropertyCount
);
