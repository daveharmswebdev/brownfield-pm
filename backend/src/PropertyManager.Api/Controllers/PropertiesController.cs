using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Application.Properties;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Property management endpoints for CRUD operations.
/// </summary>
[ApiController]
[Route("api/v1/properties")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreatePropertyCommand> _createValidator;
    private readonly IValidator<UpdatePropertyCommand> _updateValidator;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(
        IMediator mediator,
        IValidator<CreatePropertyCommand> createValidator,
        IValidator<UpdatePropertyCommand> updateValidator,
        ILogger<PropertiesController> logger)
    {
        _mediator = mediator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all properties for the current user (AC-2.1.4, AC-2.2.6).
    /// </summary>
    /// <param name="year">Optional tax year filter for expense/income totals</param>
    /// <returns>List of properties with summary information</returns>
    /// <response code="200">Returns the list of properties</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllPropertiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllProperties([FromQuery] int? year = null)
    {
        var query = new GetAllPropertiesQuery(year);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} properties for year {Year} at {Timestamp}",
            response.TotalCount,
            year?.ToString() ?? "all",
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Get a single property by ID (AC-2.3.2, AC-2.3.5, AC-2.3.6, AC-3.5.6).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <param name="year">Optional tax year filter for expense totals (defaults to current year)</param>
    /// <returns>Property detail information</returns>
    /// <response code="200">Returns the property detail</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found or belongs to different account</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyById(Guid id, [FromQuery] int? year = null)
    {
        var query = new GetPropertyByIdQuery(id, year);
        var property = await _mediator.Send(query);

        if (property == null)
        {
            _logger.LogWarning(
                "Property not found: {PropertyId} at {Timestamp}",
                id,
                DateTime.UtcNow);

            return NotFound(new ProblemDetails
            {
                Type = "https://propertymanager.app/errors/not-found",
                Title = "Resource not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Property '{id}' does not exist",
                Instance = HttpContext.Request.Path,
                Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
            });
        }

        _logger.LogInformation(
            "Retrieved property: {PropertyId} at {Timestamp}",
            id,
            DateTime.UtcNow);

        return Ok(property);
    }

    /// <summary>
    /// Create a new property (AC-2.1.3).
    /// </summary>
    /// <param name="request">Property details</param>
    /// <returns>The newly created property's ID</returns>
    /// <response code="201">Returns the newly created property ID</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePropertyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        var command = new CreatePropertyCommand(
            request.Name,
            request.Street,
            request.City,
            request.State,
            request.ZipCode);

        // Validate command
        var validationResult = await _createValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        var propertyId = await _mediator.Send(command);

        _logger.LogInformation(
            "Property created: {PropertyId} at {Timestamp}",
            propertyId,
            DateTime.UtcNow);

        var response = new CreatePropertyResponse(propertyId);

        return CreatedAtAction(
            nameof(CreateProperty),
            new { id = propertyId },
            response);
    }

    /// <summary>
    /// Update an existing property (AC-2.4.2, AC-2.4.5).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <param name="request">Updated property details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Property updated successfully</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found or belongs to different account</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProperty(Guid id, [FromBody] UpdatePropertyRequest request)
    {
        var command = new UpdatePropertyCommand(
            id,
            request.Name,
            request.Street,
            request.City,
            request.State,
            request.ZipCode);

        // Validate command
        var validationResult = await _updateValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        await _mediator.Send(command);

        _logger.LogInformation(
            "Property updated: {PropertyId} at {Timestamp}",
            id,
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Delete a property (soft delete) (AC-2.5.2).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Property deleted successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found or belongs to different account</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProperty(Guid id)
    {
        var command = new DeletePropertyCommand(id);
        await _mediator.Send(command);

        _logger.LogInformation(
            "Property deleted: {PropertyId} at {Timestamp}",
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
/// Request model for creating a property.
/// </summary>
public record CreatePropertyRequest(
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode
);

/// <summary>
/// Response model for successful property creation.
/// </summary>
public record CreatePropertyResponse(
    Guid Id
);

/// <summary>
/// Request model for updating a property (AC-2.4.5).
/// </summary>
public record UpdatePropertyRequest(
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode
);
