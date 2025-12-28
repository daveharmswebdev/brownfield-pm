using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for GET /api/v1/expenses/check-duplicate endpoint (AC-3.6.1, AC-3.6.5).
/// Tests duplicate detection: same property + same amount + date within 24 hours (±1 day).
/// </summary>
public class ExpensesControllerCheckDuplicateTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExpensesControllerCheckDuplicateTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================================================
    // Authentication Tests
    // =====================================================

    [Fact]
    public async Task CheckDuplicate_WithoutAuth_Returns401()
    {
        // Arrange & Act
        var response = await _client.GetAsync(
            "/api/v1/expenses/check-duplicate?propertyId=00000000-0000-0000-0000-000000000001&amount=100&date=2024-12-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =====================================================
    // Validation Tests
    // =====================================================

    [Fact]
    public async Task CheckDuplicate_MissingPropertyId_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync(
            "/api/v1/expenses/check-duplicate?amount=100&date=2024-12-01",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckDuplicate_MissingAmount_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var propertyId = Guid.NewGuid();

        // Act
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&date=2024-12-01",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckDuplicate_MissingDate_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var propertyId = Guid.NewGuid();

        // Act
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=100",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckDuplicate_MissingAllParams_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/expenses/check-duplicate", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =====================================================
    // Duplicate Detection Tests (AC-3.6.1)
    // =====================================================

    [Fact]
    public async Task CheckDuplicate_DuplicateFound_ReturnsIsDuplicateTrue()
    {
        // Arrange (AC-3.6.1)
        var email = $"dup-found-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create an existing expense
        await CreateExpenseAsync(propertyId, accessToken, 127.50m, "Home Depot - Faucet", new DateOnly(2024, 12, 1));

        // Act - Check for duplicate with same property, amount, date
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=127.50&date=2024-12-01",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content.Should().NotBeNull();
        content!.IsDuplicate.Should().BeTrue();
        content.ExistingExpense.Should().NotBeNull();
        content.ExistingExpense!.Amount.Should().Be(127.50m);
        content.ExistingExpense.Description.Should().Be("Home Depot - Faucet");
    }

    [Fact]
    public async Task CheckDuplicate_NoDuplicate_ReturnsIsDuplicateFalse()
    {
        // Arrange (AC-3.6.5)
        var email = $"no-dup-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Act - Check for duplicate when no expenses exist
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=100&date=2024-12-01",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content.Should().NotBeNull();
        content!.IsDuplicate.Should().BeFalse();
        content.ExistingExpense.Should().BeNull();
    }

    [Fact]
    public async Task CheckDuplicate_DateWithin24Hours_ReturnsDuplicate()
    {
        // Arrange (AC-3.6.1 - edge case: Dec 1 vs Dec 2 = duplicate)
        var email = $"date-24hr-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create expense on Dec 1
        await CreateExpenseAsync(propertyId, accessToken, 100m, "Original", new DateOnly(2024, 12, 1));

        // Act - Check for duplicate on Dec 2 (within 24 hours)
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=100&date=2024-12-02",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content!.IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDuplicate_DateMoreThan24HoursApart_ReturnsNoDuplicate()
    {
        // Arrange (AC-3.6.5 - edge case: Dec 1 vs Dec 3 = no warning)
        var email = $"date-far-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create expense on Dec 1
        await CreateExpenseAsync(propertyId, accessToken, 100m, "Original", new DateOnly(2024, 12, 1));

        // Act - Check for duplicate on Dec 3 (more than 24 hours)
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=100&date=2024-12-03",
            accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content!.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDuplicate_DifferentAmount_ReturnsNoDuplicate()
    {
        // Arrange
        var email = $"diff-amount-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var propertyId = await CreatePropertyAsync(accessToken);

        // Create expense with 100
        await CreateExpenseAsync(propertyId, accessToken, 100m, "Original", new DateOnly(2024, 12, 1));

        // Act - Check with different amount
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={propertyId}&amount=150&date=2024-12-01",
            accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content!.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDuplicate_DifferentProperty_ReturnsNoDuplicate()
    {
        // Arrange
        var email = $"diff-prop-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        var property1 = await CreatePropertyAsync(accessToken, "Property One");
        var property2 = await CreatePropertyAsync(accessToken, "Property Two");

        // Create expense on property 1
        await CreateExpenseAsync(property1, accessToken, 100m, "Original", new DateOnly(2024, 12, 1));

        // Act - Check on property 2
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={property2}&amount=100&date=2024-12-01",
            accessToken);

        // Assert
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content!.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDuplicate_OtherUserExpense_NotDetected()
    {
        // Arrange - Account isolation
        var email1 = $"dup-user1-{Guid.NewGuid():N}@example.com";
        var email2 = $"dup-user2-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates expense
        var property1 = await CreatePropertyAsync(accessToken1);
        await CreateExpenseAsync(property1, accessToken1, 100m, "User 1 Expense", new DateOnly(2024, 12, 1));

        // User 2 creates their own property
        var property2 = await CreatePropertyAsync(accessToken2);

        // Act - User 2 checks for duplicate (should not see User 1's expense)
        var response = await GetWithAuthAsync(
            $"/api/v1/expenses/check-duplicate?propertyId={property2}&amount=100&date=2024-12-01",
            accessToken2);

        // Assert - No duplicate found (other user's expense not visible)
        var content = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>();
        content!.IsDuplicate.Should().BeFalse();
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

// Response record for duplicate check deserialization
public record DuplicateCheckResponse(
    bool IsDuplicate,
    DuplicateExpenseResponse? ExistingExpense
);

public record DuplicateExpenseResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string? Description
);
