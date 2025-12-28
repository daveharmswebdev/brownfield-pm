using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

/// <summary>
/// Integration tests for PropertiesController (AC-2.1.1, AC-2.1.3, AC-2.1.4, AC-2.1.5).
/// </summary>
public class PropertiesControllerTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PropertiesControllerTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProperty_WithoutAuth_Returns401()
    {
        // Arrange
        var request = new
        {
            Name = "Oak Street Duplex",
            Street = "123 Oak Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/properties", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProperty_WithValidData_Returns201WithId()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        var request = new
        {
            Name = "Oak Street Duplex",
            Street = "123 Oak Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken!);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>();
        content.Should().NotBeNull();
        content!.Id.Should().NotBe(Guid.Empty);

        // Verify Location header is present
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProperty_WithMissingName_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        var request = new
        {
            Name = "",
            Street = "123 Oak Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Name");
    }

    [Fact]
    public async Task CreateProperty_WithInvalidState_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        var request = new
        {
            Name = "Oak Street Duplex",
            Street = "123 Oak Street",
            City = "Austin",
            State = "Texas", // Should be 2 characters
            ZipCode = "78701"
        };

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("State");
    }

    [Fact]
    public async Task CreateProperty_WithInvalidZipCode_Returns400()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        var request = new
        {
            Name = "Oak Street Duplex",
            Street = "123 Oak Street",
            City = "Austin",
            State = "TX",
            ZipCode = "ABCDE" // Should be 5 digits
        };

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ZipCode");
    }

    [Fact]
    public async Task CreateProperty_CreatesWithCorrectAccountId()
    {
        // Arrange
        var email = $"property-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var request = new
        {
            Name = "Multi-Tenant Test Property",
            Street = "789 Test Street",
            City = "Dallas",
            State = "TX",
            ZipCode = "75201"
        };

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);
        var content = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the property was created with the correct account
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var property = await dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == content!.Id);

        property.Should().NotBeNull();
        property!.Name.Should().Be("Multi-Tenant Test Property");
        property.Street.Should().Be("789 Test Street");
        property.City.Should().Be("Dallas");
        property.State.Should().Be("TX");
        property.ZipCode.Should().Be("75201");
        property.AccountId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateProperty_SetsTimestamps()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        var request = new
        {
            Name = "Timestamp Test Property",
            Street = "101 Time Street",
            City = "Houston",
            State = "TX",
            ZipCode = "77001"
        };

        var beforeCreate = DateTime.UtcNow;

        // Act
        var response = await PostAsJsonWithAuthAsync("/api/v1/properties", request, accessToken);
        var content = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        var afterCreate = DateTime.UtcNow;

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var property = await dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == content!.Id);

        property.Should().NotBeNull();
        property!.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        property.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
        property.UpdatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        property.UpdatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);
        return accessToken;
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginAsync(string email)
    {
        var password = "Test@123456";

        // Register
        var registerRequest = new
        {
            Email = email,
            Password = password,
            Name = "Test Account"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        // Verify email using fake email service
        using var scope = _factory.Services.CreateScope();
        var fakeEmailService = scope.ServiceProvider.GetRequiredService<FakeEmailService>();
        var verificationToken = fakeEmailService.SentVerificationEmails.Last().Token;

        var verifyRequest = new { Token = verificationToken };
        var verifyResponse = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyRequest);
        verifyResponse.EnsureSuccessStatusCode();

        // Login
        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return (loginContent!.AccessToken, Guid.Empty);
    }

    [Fact]
    public async Task GetAllProperties_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/properties");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllProperties_WithNoProperties_ReturnsEmptyList()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/properties", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<GetAllPropertiesResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().BeEmpty();
        content.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllProperties_WithProperties_ReturnsPropertyList()
    {
        // Arrange
        var email = $"property-list-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property first
        var createRequest = new
        {
            Name = "Test Property for List",
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);

        // Act
        var response = await GetWithAuthAsync("/api/v1/properties", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<GetAllPropertiesResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(1);
        content.TotalCount.Should().Be(1);

        var property = content.Items[0];
        property.Name.Should().Be("Test Property for List");
        property.City.Should().Be("Austin");
        property.State.Should().Be("TX");
        property.ExpenseTotal.Should().Be(0);
        property.IncomeTotal.Should().Be(0);
    }

    [Fact]
    public async Task GetAllProperties_WithYearParameter_Returns200()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();

        // Act
        var response = await GetWithAuthAsync("/api/v1/properties?year=2024", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<GetAllPropertiesResponse>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllProperties_OnlyReturnsOwnProperties()
    {
        // Arrange - Create properties with two different users
        var email1 = $"user1-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates a property
        var createRequest1 = new
        {
            Name = "User1 Property",
            Street = "123 User1 Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest1, accessToken1);

        // User 2 creates a property
        var createRequest2 = new
        {
            Name = "User2 Property",
            Street = "456 User2 Street",
            City = "Dallas",
            State = "TX",
            ZipCode = "75201"
        };
        await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest2, accessToken2);

        // Act - User 1 gets their properties
        var response = await GetWithAuthAsync("/api/v1/properties", accessToken1);

        // Assert - Should only see User1's property
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<GetAllPropertiesResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Name.Should().Be("User1 Property");
    }

    [Fact]
    public async Task GetPropertyById_WithoutAuth_Returns401()
    {
        // Arrange
        var propertyId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/properties/{propertyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPropertyById_ValidId_Returns200WithPropertyDetail()
    {
        // Arrange
        var email = $"property-detail-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property first
        var createRequest = new
        {
            Name = "Detail Test Property",
            Street = "123 Detail Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Act
        var response = await GetWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<PropertyDetailDto>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(createContent.Id);
        content.Name.Should().Be("Detail Test Property");
        content.Street.Should().Be("123 Detail Street");
        content.City.Should().Be("Austin");
        content.State.Should().Be("TX");
        content.ZipCode.Should().Be("78701");
        content.ExpenseTotal.Should().Be(0);
        content.IncomeTotal.Should().Be(0);
        content.RecentExpenses.Should().BeEmpty();
        content.RecentIncome.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPropertyById_InvalidGuid_Returns404()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetWithAuthAsync($"/api/v1/properties/{nonExistentId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPropertyById_OtherAccountProperty_Returns404()
    {
        // Arrange - Create property with user 1, try to access with user 2
        var email1 = $"user1-detail-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-detail-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates a property
        var createRequest = new
        {
            Name = "User1 Detail Property",
            Street = "123 User1 Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken1);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Act - User 2 tries to access User 1's property
        var response = await GetWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken2);

        // Assert - Should return 404 (not 403) to prevent data leakage
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPropertyById_ReturnsTimestamps()
    {
        // Arrange
        var email = $"timestamp-detail-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        var beforeCreate = DateTime.UtcNow;

        // Create a property
        var createRequest = new
        {
            Name = "Timestamp Detail Property",
            Street = "123 Time Street",
            City = "Houston",
            State = "TX",
            ZipCode = "77001"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        var afterCreate = DateTime.UtcNow;

        // Act
        var response = await GetWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<PropertyDetailDto>();
        content.Should().NotBeNull();
        content!.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        content.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
        content.UpdatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        content.UpdatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
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

    private async Task<HttpResponseMessage> PutAsJsonWithAuthAsync<T>(string url, T content, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(content);
        return await _client.SendAsync(request);
    }

    // =====================================================
    // PUT /api/v1/properties/{id} Tests (AC-2.4.5)
    // =====================================================

    [Fact]
    public async Task UpdateProperty_WithoutAuth_Returns401()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var request = new
        {
            Name = "Updated Property",
            Street = "123 Updated Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/properties/{propertyId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProperty_ValidData_Returns204()
    {
        // Arrange
        var email = $"update-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property first
        var createRequest = new
        {
            Name = "Original Property Name",
            Street = "123 Original Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Update request
        var updateRequest = new
        {
            Name = "Updated Property Name",
            Street = "456 Updated Street",
            City = "Dallas",
            State = "CA",
            ZipCode = "90210"
        };

        // Act
        var response = await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent!.Id}", updateRequest, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProperty_ValidData_UpdatesAllFields()
    {
        // Arrange
        var email = $"update-fields-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Original Name",
            Street = "Original Street",
            City = "Original City",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Update request
        var updateRequest = new
        {
            Name = "New Name",
            Street = "New Street",
            City = "New City",
            State = "CA",
            ZipCode = "90210"
        };

        // Act
        await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent!.Id}", updateRequest, accessToken);

        // Fetch the updated property
        var getResponse = await GetWithAuthAsync($"/api/v1/properties/{createContent.Id}", accessToken);
        var updatedProperty = await getResponse.Content.ReadFromJsonAsync<PropertyDetailDto>();

        // Assert
        updatedProperty.Should().NotBeNull();
        updatedProperty!.Name.Should().Be("New Name");
        updatedProperty.Street.Should().Be("New Street");
        updatedProperty.City.Should().Be("New City");
        updatedProperty.State.Should().Be("CA");
        updatedProperty.ZipCode.Should().Be("90210");
    }

    [Fact]
    public async Task UpdateProperty_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var email = $"update-timestamp-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Timestamp Test",
            Street = "123 Time Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Get original timestamps
        var originalResponse = await GetWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);
        var original = await originalResponse.Content.ReadFromJsonAsync<PropertyDetailDto>();
        var originalUpdatedAt = original!.UpdatedAt;

        // Wait a moment to ensure timestamp difference
        await Task.Delay(100);

        // Update
        var updateRequest = new
        {
            Name = "Updated Timestamp Test",
            Street = "123 Time Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent.Id}", updateRequest, accessToken);

        // Act
        var updatedResponse = await GetWithAuthAsync($"/api/v1/properties/{createContent.Id}", accessToken);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<PropertyDetailDto>();

        // Assert
        updated!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        updated.CreatedAt.Should().Be(original.CreatedAt);
    }

    [Fact]
    public async Task UpdateProperty_NonExistentProperty_Returns404()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var nonExistentId = Guid.NewGuid();

        var updateRequest = new
        {
            Name = "Update Non-Existent",
            Street = "123 Ghost Street",
            City = "Nowhere",
            State = "TX",
            ZipCode = "00000"
        };

        // Act
        var response = await PutAsJsonWithAuthAsync($"/api/v1/properties/{nonExistentId}", updateRequest, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProperty_OtherAccountProperty_Returns404()
    {
        // Arrange
        var email1 = $"user1-update-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-update-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates a property
        var createRequest = new
        {
            Name = "User1 Property",
            Street = "123 User1 Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken1);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // User 2 tries to update User 1's property
        var updateRequest = new
        {
            Name = "Hacked Property",
            Street = "123 Hacker Street",
            City = "Hackerville",
            State = "TX",
            ZipCode = "00000"
        };

        // Act
        var response = await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent!.Id}", updateRequest, accessToken2);

        // Assert - Should return 404 (not 403) to prevent data leakage
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProperty_InvalidState_Returns400()
    {
        // Arrange
        var email = $"update-invalid-state-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Test Property",
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Update with invalid state
        var updateRequest = new
        {
            Name = "Updated Property",
            Street = "123 Test Street",
            City = "Austin",
            State = "Texas", // Invalid - should be 2 chars
            ZipCode = "78701"
        };

        // Act
        var response = await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent!.Id}", updateRequest, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("State");
    }

    [Fact]
    public async Task UpdateProperty_InvalidZipCode_Returns400()
    {
        // Arrange
        var email = $"update-invalid-zip-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Test Property",
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Update with invalid ZIP
        var updateRequest = new
        {
            Name = "Updated Property",
            Street = "123 Test Street",
            City = "Austin",
            State = "TX",
            ZipCode = "ABCDE" // Invalid - should be 5 digits
        };

        // Act
        var response = await PutAsJsonWithAuthAsync($"/api/v1/properties/{createContent!.Id}", updateRequest, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ZipCode");
    }
    // =====================================================
    // DELETE /api/v1/properties/{id} Tests (AC-2.5.2, AC-2.5.3, AC-2.5.5)
    // =====================================================

    [Fact]
    public async Task DeleteProperty_WithoutAuth_Returns401()
    {
        // Arrange
        var propertyId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/properties/{propertyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProperty_ValidProperty_Returns204()
    {
        // Arrange
        var email = $"delete-test-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property first
        var createRequest = new
        {
            Name = "Property to Delete",
            Street = "123 Doomed Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Act
        var response = await DeleteWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProperty_ValidProperty_SetsDeletedAt()
    {
        // Arrange
        var email = $"delete-timestamp-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Delete Timestamp Test",
            Street = "123 Time Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        var beforeDelete = DateTime.UtcNow;

        // Act
        await DeleteWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        var afterDelete = DateTime.UtcNow;

        // Assert - verify soft delete in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var property = await dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == createContent.Id);

        property.Should().NotBeNull();
        property!.DeletedAt.Should().NotBeNull();
        property.DeletedAt.Should().BeAfter(beforeDelete.AddSeconds(-1));
        property.DeletedAt.Should().BeBefore(afterDelete.AddSeconds(1));
    }

    [Fact]
    public async Task DeleteProperty_NonExistentProperty_Returns404()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await DeleteWithAuthAsync($"/api/v1/properties/{nonExistentId}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProperty_OtherAccountProperty_Returns404()
    {
        // Arrange
        var email1 = $"user1-delete-{Guid.NewGuid():N}@example.com";
        var email2 = $"user2-delete-{Guid.NewGuid():N}@example.com";
        var (accessToken1, _) = await RegisterAndLoginAsync(email1);
        var (accessToken2, _) = await RegisterAndLoginAsync(email2);

        // User 1 creates a property
        var createRequest = new
        {
            Name = "User1 Property",
            Street = "123 User1 Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken1);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Act - User 2 tries to delete User 1's property
        var response = await DeleteWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken2);

        // Assert - Should return 404 (not 403) to prevent data leakage
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProperty_AlreadyDeleted_Returns404()
    {
        // Arrange
        var email = $"delete-twice-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new
        {
            Name = "Double Delete Test",
            Street = "123 Ghost Street",
            City = "Austin",
            State = "TX",
            ZipCode = "78701"
        };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Delete it once
        await DeleteWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        // Act - Try to delete again
        var response = await DeleteWithAuthAsync($"/api/v1/properties/{createContent.Id}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProperty_ExcludedFromGetAllProperties()
    {
        // Arrange (AC-2.5.5)
        var email = $"delete-list-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create two properties
        var createRequest1 = new { Name = "Property to Keep", Street = "123 Keep Street", City = "Austin", State = "TX", ZipCode = "78701" };
        var createRequest2 = new { Name = "Property to Delete", Street = "456 Delete Street", City = "Austin", State = "TX", ZipCode = "78702" };

        await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest1, accessToken);
        var createResponse2 = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest2, accessToken);
        var deleteContent = await createResponse2.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Delete one property
        await DeleteWithAuthAsync($"/api/v1/properties/{deleteContent!.Id}", accessToken);

        // Act
        var response = await GetWithAuthAsync("/api/v1/properties", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GetAllPropertiesResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().HaveCount(1);
        content.Items[0].Name.Should().Be("Property to Keep");
    }

    [Fact]
    public async Task DeleteProperty_GetByIdReturns404()
    {
        // Arrange (AC-2.5.5)
        var email = $"delete-getbyid-{Guid.NewGuid():N}@example.com";
        var (accessToken, _) = await RegisterAndLoginAsync(email);

        // Create a property
        var createRequest = new { Name = "Property to Delete", Street = "123 Delete Street", City = "Austin", State = "TX", ZipCode = "78701" };
        var createResponse = await PostAsJsonWithAuthAsync("/api/v1/properties", createRequest, accessToken);
        var createContent = await createResponse.Content.ReadFromJsonAsync<CreatePropertyResponse>();

        // Delete it
        await DeleteWithAuthAsync($"/api/v1/properties/{createContent!.Id}", accessToken);

        // Act - Try to get deleted property
        var response = await GetWithAuthAsync($"/api/v1/properties/{createContent.Id}", accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> DeleteWithAuthAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return await _client.SendAsync(request);
    }
}

public record CreatePropertyResponse(Guid Id);
public record LoginResponse(string AccessToken, int ExpiresIn);

public record GetAllPropertiesResponse(IReadOnlyList<PropertySummaryDto> Items, int TotalCount);

public record PropertySummaryDto(
    Guid Id,
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode,
    decimal ExpenseTotal,
    decimal IncomeTotal
);

public record PropertyDetailDto(
    Guid Id,
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode,
    decimal ExpenseTotal,
    decimal IncomeTotal,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ExpenseSummaryDto> RecentExpenses,
    IReadOnlyList<IncomeSummaryDto> RecentIncome
);

public record ExpenseSummaryDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date
);

public record IncomeSummaryDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime Date
);
