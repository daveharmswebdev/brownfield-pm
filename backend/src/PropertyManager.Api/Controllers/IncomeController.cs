using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Application.Income;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Income management endpoints for CRUD operations (AC-4.1.1, AC-4.1.3, AC-4.2.2, AC-4.2.3, AC-4.2.6).
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class IncomeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreateIncomeCommand> _createValidator;
    private readonly IValidator<UpdateIncomeCommand> _updateValidator;
    private readonly ILogger<IncomeController> _logger;

    public IncomeController(
        IMediator mediator,
        IValidator<CreateIncomeCommand> createValidator,
        IValidator<UpdateIncomeCommand> updateValidator,
        ILogger<IncomeController> logger)
    {
        _mediator = mediator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all income across all properties with optional filters (AC-4.3.1, AC-4.3.2, AC-4.3.3, AC-4.3.4, AC-4.3.6).
    /// </summary>
    /// <param name="dateFrom">Filter income from this date (inclusive)</param>
    /// <param name="dateTo">Filter income to this date (inclusive)</param>
    /// <param name="propertyId">Filter to specific property</param>
    /// <param name="year">Filter to specific tax year</param>
    /// <returns>List of income entries with total count and amount</returns>
    /// <response code="200">Returns the list of income entries with totals</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("income")]
    [ProducesResponseType(typeof(IncomeListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllIncome(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] Guid? propertyId = null,
        [FromQuery] int? year = null)
    {
        var query = new GetAllIncomeQuery(dateFrom, dateTo, propertyId, year);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} income entries (total: {Total}), filters: dateFrom={DateFrom}, dateTo={DateTo}, propertyId={PropertyId}, year={Year} at {Timestamp}",
            response.TotalCount,
            response.TotalAmount,
            dateFrom?.ToString() ?? "null",
            dateTo?.ToString() ?? "null",
            propertyId?.ToString() ?? "null",
            year?.ToString() ?? "null",
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Create a new income entry (AC-4.1.3).
    /// </summary>
    /// <param name="request">Income details</param>
    /// <returns>The newly created income's ID</returns>
    /// <response code="201">Returns the newly created income ID</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found</response>
    [HttpPost("income")]
    [ProducesResponseType(typeof(CreateIncomeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIncome([FromBody] CreateIncomeRequest request)
    {
        var command = new CreateIncomeCommand(
            request.PropertyId,
            request.Amount,
            request.Date,
            request.Source,
            request.Description);

        // Validate command
        var validationResult = await _createValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        var incomeId = await _mediator.Send(command);

        _logger.LogInformation(
            "Income created: {IncomeId} for property {PropertyId}, amount {Amount} at {Timestamp}",
            incomeId,
            request.PropertyId,
            request.Amount,
            DateTime.UtcNow);

        var response = new CreateIncomeResponse(incomeId);

        return CreatedAtAction(
            nameof(CreateIncome),
            new { id = incomeId },
            response);
    }

    /// <summary>
    /// Get income for a property (AC-4.1.2, AC-4.1.6).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <param name="year">Optional tax year filter</param>
    /// <returns>List of income entries with YTD total</returns>
    /// <response code="200">Returns the list of income entries</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found</response>
    [HttpGet("properties/{id:guid}/income")]
    [ProducesResponseType(typeof(IncomeListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeByProperty(Guid id, [FromQuery] int? year = null)
    {
        var query = new GetIncomeByPropertyQuery(id, year);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} income entries for property {PropertyId}, year {Year} at {Timestamp}",
            response.TotalCount,
            id,
            year?.ToString() ?? "all",
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Get income total for a property and year (AC-4.1.4).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <param name="year">Tax year</param>
    /// <returns>Total income amount</returns>
    /// <response code="200">Returns the total income amount</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found</response>
    [HttpGet("properties/{id:guid}/income/total")]
    [ProducesResponseType(typeof(IncomeTotalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeTotalByProperty(Guid id, [FromQuery] int? year = null)
    {
        var effectiveYear = year ?? DateTime.UtcNow.Year;
        var query = new GetIncomeTotalByPropertyQuery(id, effectiveYear);
        var total = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved income total for property {PropertyId}, year {Year}: {Total} at {Timestamp}",
            id,
            effectiveYear,
            total,
            DateTime.UtcNow);

        return Ok(new IncomeTotalResponse(total, effectiveYear));
    }

    /// <summary>
    /// Get a single income entry by ID (AC-4.2.2).
    /// </summary>
    /// <param name="id">Income GUID</param>
    /// <returns>Income entry details</returns>
    /// <response code="200">Returns the income entry</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If income not found</response>
    [HttpGet("income/{id:guid}")]
    [ProducesResponseType(typeof(IncomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeById(Guid id)
    {
        var query = new GetIncomeByIdQuery(id);
        var income = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved income: {IncomeId} at {Timestamp}",
            id,
            DateTime.UtcNow);

        return Ok(income);
    }

    /// <summary>
    /// Update an existing income entry (AC-4.2.2, AC-4.2.3, AC-4.2.4).
    /// </summary>
    /// <param name="id">Income GUID</param>
    /// <param name="request">Updated income details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Income updated successfully</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If income not found</response>
    [HttpPut("income/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIncome(Guid id, [FromBody] UpdateIncomeRequest request)
    {
        var command = new UpdateIncomeCommand(
            id,
            request.Amount,
            request.Date,
            request.Source,
            request.Description);

        // Validate command
        var validationResult = await _updateValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        await _mediator.Send(command);

        _logger.LogInformation(
            "Income updated: {IncomeId}, amount {Amount} at {Timestamp}",
            id,
            request.Amount,
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Delete an income entry (soft delete) (AC-4.2.5, AC-4.2.6).
    /// </summary>
    /// <param name="id">Income GUID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Income deleted successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If income not found</response>
    [HttpDelete("income/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncome(Guid id)
    {
        var command = new DeleteIncomeCommand(id);
        await _mediator.Send(command);

        _logger.LogInformation(
            "Income deleted: {IncomeId} at {Timestamp}",
            id,
            DateTime.UtcNow);

        return NoContent();
    }

    private ValidationProblemDetails CreateValidationProblemDetails(FluentValidation.Results.ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path,
            Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
        };
    }
}

/// <summary>
/// Request model for creating an income entry.
/// </summary>
public record CreateIncomeRequest(
    Guid PropertyId,
    decimal Amount,
    DateOnly Date,
    string? Source,
    string? Description
);

/// <summary>
/// Response model for successful income creation.
/// </summary>
public record CreateIncomeResponse(
    Guid Id
);

/// <summary>
/// Response model for income total.
/// </summary>
public record IncomeTotalResponse(
    decimal Total,
    int Year
);

/// <summary>
/// Request model for updating an income entry (AC-4.2.2).
/// </summary>
public record UpdateIncomeRequest(
    decimal Amount,
    DateOnly Date,
    string? Source,
    string? Description
);
