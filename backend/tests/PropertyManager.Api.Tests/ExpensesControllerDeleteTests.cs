using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for ExpensesController DELETE endpoint (AC-3.3.1, AC-3.3.3, AC-3.3.5).
/// </summary>
public class ExpensesControllerDeleteTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExpensesControllerDeleteTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // =====================================================
    // DELETE /api/v1/expenses/{id} Tests (AC-3.3.1, AC-3.3.3, AC-3.3.5)
    // =====================================================

    [Fact]
    public async Task DeleteExpense_WithoutAuth_Returns401()
    {
        // Arrange
        var expenseId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/expenses/{expenseId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteExpense_ValidExpense_Returns204()
    {
        // Arrange
        var email = $"delete-expense-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken);
        var expenseId = await CreateExpenseAsync(propertyId, accessToken);

        // Act
        var response = await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteExpense_ValidExpense_SetsDeletedAt()
    {
        // Arrange
        var email = $"delete-timestamp-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken);
        var expenseId = await CreateExpenseAsync(propertyId, accessToken);

        var beforeDelete = DateTime.UtcNow;

        // Act
        await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        var afterDelete = DateTime.UtcNow;

        // Assert - verify soft delete in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expense = await dbContext.Expenses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        expense.Should().NotBeNull();
        expense!.DeletedAt.Should().NotBeNull();
        expense.DeletedAt.Should().BeAfter(beforeDelete.AddSeconds(-1));
        expense.DeletedAt.Should().BeBefore(afterDelete.AddSeconds(1));
    }

    [Fact]
    public async Task DeleteExpense_NonExistentExpense_Returns404()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await DeleteWithAuthAsync($"/api/v1/expenses/{nonExistentId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExpense_OtherAccountExpense_Returns404()
    {
        // Arrange - Two users
        var email1 = $"user1-expense-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-expense-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates a property and expense
        var propertyId = await CreatePropertyAsync(accessToken1);
        var expenseId = await CreateExpenseAsync(propertyId, accessToken1);

        // Act - User 2 tries to delete User 1's expense
        var response = await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken2);

        // Assert - Should return 404 (not 403) to prevent data leakage
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExpense_AlreadyDeleted_Returns404()
    {
        // Arrange
        var email = $"delete-twice-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken);
        var expenseId = await CreateExpenseAsync(propertyId, accessToken);

        // Delete it once
        await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        // Act - Try to delete again
        var response = await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExpense_ExcludedFromGetByProperty()
    {
        // Arrange (AC-3.3.5)
        var email = $"delete-list-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken);

        // Create two expenses
        var expenseId1 = await CreateExpenseAsync(propertyId, accessToken, 100.00m, "Expense to Keep");
        var expenseId2 = await CreateExpenseAsync(propertyId, accessToken, 50.00m, "Expense to Delete");

        // Delete one expense
        await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId2}", accessToken);

        // Act
        var response = await GetWithAuthAsync($"/api/v1/properties/{propertyId}/expenses", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ExpenseListDto>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Id.Should().Be(expenseId1);
        content.Items[0].Description.Should().Be("Expense to Keep");
        content.YtdTotal.Should().Be(100.00m); // Only the non-deleted expense counts
    }

    [Fact]
    public async Task DeleteExpense_GetByIdReturns404()
    {
        // Arrange (AC-3.3.5)
        var email = $"delete-getbyid-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var propertyId = await CreatePropertyAsync(accessToken);
        var expenseId = await CreateExpenseAsync(propertyId, accessToken);

        // Delete it
        await DeleteWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        // Act - Try to get deleted expense
        var response = await GetWithAuthAsync($"/api/v1/expenses/{expenseId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task<Guid> CreatePropertyAsync(string accessToken)
    {
        var createRequest = new
        {
            Name = $"Test Property {Guid.NewGuid():N}",
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
        string description = "Test Expense")
    {
        // Get a category ID (seeded data)
        var categoriesResponse = await GetWithAuthAsync("/api/v1/expense-categories", accessToken);
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<ExpenseCategoriesResponse>();
        var categoryId = categories!.Items[0].Id;

        var createRequest = new
        {
            PropertyId = propertyId,
            Amount = amount,
            Date = DateOnly.FromDateTime(DateTime.Today),
            CategoryId = categoryId,
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

    private async Task<HttpResponseMessage> DeleteWithAuthAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return await _client.SendAsync(request);
    }
}

// Response records for deserialization
public record CreateExpenseResponse(Guid Id);
public record ExpenseCategoriesResponse(List<ExpenseCategoryDto> Items, int TotalCount);
