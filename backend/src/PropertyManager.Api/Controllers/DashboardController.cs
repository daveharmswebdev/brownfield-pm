using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Application.Dashboard;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Dashboard endpoints for aggregated financial data (AC-4.4.1, AC-4.4.2).
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard totals for the specified tax year (AC-4.4.1, AC-4.4.2, AC-4.4.6).
    /// Returns aggregated expenses, income, net income, and property count.
    /// </summary>
    /// <param name="year">Tax year to aggregate totals for (required)</param>
    /// <returns>Dashboard totals including expenses, income, net income, and property count</returns>
    /// <response code="200">Returns the dashboard totals</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("totals")]
    [ProducesResponseType(typeof(DashboardTotalsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTotals([FromQuery] int year)
    {
        var query = new GetDashboardTotalsQuery(year);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved dashboard totals for year {Year}: Expenses={Expenses}, Income={Income}, Net={Net} at {Timestamp}",
            year,
            response.TotalExpenses,
            response.TotalIncome,
            response.NetIncome,
            DateTime.UtcNow);

        return Ok(response);
    }
}
