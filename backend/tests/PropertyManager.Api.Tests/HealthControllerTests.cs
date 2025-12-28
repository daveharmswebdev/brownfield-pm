using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PropertyManager.Api.Tests;

public class HealthControllerTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(PropertyManagerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOkWithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<HealthResponseDto>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("healthy");
        content.Version.Should().NotBeNullOrEmpty();
        content.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HealthReady_WhenDatabaseConnected_ReturnsOkWithReadyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ReadyResponseDto>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("ready");
        content.Database.Should().Be("connected");
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthentication()
    {
        // Act - no auth headers
        var response = await _client.GetAsync("/api/v1/health");

        // Assert - should not be 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthReady_DoesNotRequireAuthentication()
    {
        // Act - no auth headers
        var response = await _client.GetAsync("/api/v1/health/ready");

        // Assert - should not be 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    private record HealthResponseDto(string Status, string Version, DateTime Timestamp);
    private record ReadyResponseDto(string Status, string Database, DateTime Timestamp);
}
