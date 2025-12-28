using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Health check endpoints for deployment verification and monitoring.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint. Returns application status and version.
    /// </summary>
    /// <returns>Health status with version information.</returns>
    /// <response code="200">Application is healthy.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new HealthResponse
        {
            Status = "healthy",
            Version = version,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness check endpoint. Verifies database connectivity.
    /// </summary>
    /// <returns>Ready status if database is accessible.</returns>
    /// <response code="200">Application is ready to accept traffic.</response>
    /// <response code="503">Application is not ready (database unavailable).</response>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(ReadyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadyResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return Ok(new ReadyResponse
                {
                    Status = "ready",
                    Database = "connected",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogWarning("Database readiness check failed: unable to connect");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ReadyResponse
            {
                Status = "not ready",
                Database = "disconnected",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database readiness check failed with exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ReadyResponse
            {
                Status = "not ready",
                Database = "error",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Health check response model.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Current health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Readiness check response model.
/// </summary>
public class ReadyResponse
{
    /// <summary>
    /// Current readiness status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Database connection status.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the readiness check.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
