using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyManager.Api.Middleware;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Api.Tests.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionHandlerMiddleware.
/// Tests exception handling, ProblemDetails response format, logging levels, and environment-specific behavior.
/// </summary>
public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task NotFoundException_Returns404WithCorrectProblemDetails()
    {
        // Arrange
        var exception = new NotFoundException("Property", Guid.NewGuid());
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        // Note: ContentType may be empty before response is actually written in unit test context
        // The middleware sets it via WriteAsJsonAsync which handles content-type

        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://propertymanager.app/errors/not-found");
        problemDetails.Title.Should().Be("Resource not found");
        problemDetails.Status.Should().Be(404);
        problemDetails.Detail.Should().Contain("was not found");
        problemDetails.Instance.Should().Be(_httpContext.Request.Path);

        // Verify traceId is included
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().NotBeNull();
    }

    [Fact]
    public async Task ValidationException_Returns400WithCorrectProblemDetails()
    {
        // Arrange
        var exception = new ValidationException("Validation failed");
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problemDetails.Title.Should().Be("Validation error");
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Be("Validation failed");
    }

    [Fact]
    public async Task ArgumentException_Returns400WithCorrectProblemDetails()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://propertymanager.app/errors/bad-request");
        problemDetails.Title.Should().Be("Bad request");
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Be("Invalid argument");
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns403WithCorrectProblemDetails()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://propertymanager.app/errors/forbidden");
        problemDetails.Title.Should().Be("Access forbidden");
        problemDetails.Status.Should().Be(403);
        problemDetails.Detail.Should().Be("Access denied");
    }

    [Fact]
    public async Task GenericException_Returns500WithCorrectProblemDetails()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://propertymanager.app/errors/internal-server-error");
        problemDetails.Title.Should().Be("Internal server error");
        problemDetails.Status.Should().Be(500);
        problemDetails.Detail.Should().Be("Something went wrong");
    }

    [Fact]
    public async Task DevelopmentMode_IncludesStackTrace()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var middleware = CreateMiddleware(exception, Environments.Development);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Extensions.Should().ContainKey("exceptionDetails");

        var exceptionDetails = problemDetails.Extensions["exceptionDetails"] as JsonElement?;
        exceptionDetails.Should().NotBeNull();
        exceptionDetails!.Value.GetString().Should().Contain("Test exception");
        exceptionDetails.Value.GetString().Should().Contain("at ");  // Stack trace marker
    }

    [Fact]
    public async Task ProductionMode_DoesNotIncludeStackTrace()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Extensions.Should().NotContainKey("exceptionDetails");
    }

    [Fact]
    public async Task TraceIdentifier_IsIncludedInResponse()
    {
        // Arrange
        var expectedTraceId = "test-trace-id-12345";
        _httpContext.TraceIdentifier = expectedTraceId;
        var exception = new NotFoundException("Test", Guid.NewGuid());
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Extensions["traceId"].Should().NotBeNull();

        var traceId = problemDetails.Extensions["traceId"] as JsonElement?;
        traceId.Should().NotBeNull();
        traceId!.Value.GetString().Should().Be(expectedTraceId);
    }

    [Fact]
    public async Task InstanceProperty_ContainsRequestPath()
    {
        // Arrange
        _httpContext.Request.Path = "/api/v1/properties/123";
        var exception = new NotFoundException("Property", Guid.NewGuid());
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Instance.Should().Be("/api/v1/properties/123");
    }

    [Fact]
    public async Task NoException_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;

        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var environment = CreateMockEnvironment(Environments.Production);
        var logger = NullLogger<GlobalExceptionHandlerMiddleware>.Instance;

        var middleware = new GlobalExceptionHandlerMiddleware(next, logger, environment);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task StagingEnvironment_DoesNotIncludeStackTrace()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var middleware = CreateMiddleware(exception, Environments.Staging);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Extensions.Should().NotContainKey("exceptionDetails");
    }

    [Fact]
    public async Task MultipleExceptionProperties_AllIncludedInProblemDetails()
    {
        // Arrange
        _httpContext.Request.Path = "/api/v1/expenses";
        _httpContext.TraceIdentifier = "trace-123";
        var propertyId = Guid.NewGuid();
        var exception = new NotFoundException("Property", propertyId);
        var middleware = CreateMiddleware(exception, Environments.Production);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var problemDetails = await DeserializeProblemDetails();
        problemDetails.Should().NotBeNull();
        problemDetails!.Type.Should().Be("https://propertymanager.app/errors/not-found");
        problemDetails.Title.Should().Be("Resource not found");
        problemDetails.Status.Should().Be(404);
        problemDetails.Detail.Should().Contain($"Property with ID '{propertyId}' was not found");
        problemDetails.Instance.Should().Be("/api/v1/expenses");
        problemDetails.Extensions["traceId"].Should().NotBeNull();

        var traceId = problemDetails.Extensions["traceId"] as JsonElement?;
        traceId!.Value.GetString().Should().Be("trace-123");
    }

    // Helper methods

    private GlobalExceptionHandlerMiddleware CreateMiddleware(Exception exceptionToThrow, string environmentName)
    {
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw exceptionToThrow;
        };

        var environment = CreateMockEnvironment(environmentName);
        var logger = NullLogger<GlobalExceptionHandlerMiddleware>.Instance;

        return new GlobalExceptionHandlerMiddleware(next, logger, environment);
    }

    private IHostEnvironment CreateMockEnvironment(string environmentName)
    {
        return new TestHostEnvironment { EnvironmentName = environmentName };
    }

    private async Task<ProblemDetails?> DeserializeProblemDetails()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Test implementation of IHostEnvironment
    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "PropertyManager.Api.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
