using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Application.Expenses;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Api.Controllers;

/// <summary>
/// Expense management endpoints for CRUD operations.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreateExpenseCommand> _createValidator;
    private readonly IValidator<UpdateExpenseCommand> _updateValidator;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        IMediator mediator,
        IValidator<CreateExpenseCommand> createValidator,
        IValidator<UpdateExpenseCommand> updateValidator,
        ILogger<ExpensesController> logger)
    {
        _mediator = mediator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Check for potential duplicate expenses (AC-3.6.1, AC-3.6.5).
    /// Duplicate detection: same property + same amount + date within 24 hours (±1 day).
    /// </summary>
    /// <param name="propertyId">Property GUID</param>
    /// <param name="amount">Expense amount (decimal)</param>
    /// <param name="date">Expense date (DateOnly)</param>
    /// <returns>Duplicate check result with optional existing expense details</returns>
    /// <response code="200">Returns duplicate check result (isDuplicate: true/false)</response>
    /// <response code="400">If required parameters are missing</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("expenses/check-duplicate")]
    [ProducesResponseType(typeof(DuplicateCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckDuplicateExpense(
        [FromQuery] Guid? propertyId,
        [FromQuery] decimal? amount,
        [FromQuery] DateOnly? date)
    {
        // Validate required parameters
        var errors = new Dictionary<string, string[]>();
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
        {
            errors.Add("propertyId", ["Property ID is required"]);
        }
        if (!amount.HasValue)
        {
            errors.Add("amount", ["Amount is required"]);
        }
        if (!date.HasValue)
        {
            errors.Add("date", ["Date is required"]);
        }

        if (errors.Count > 0)
        {
            var problemDetails = new ValidationProblemDetails(errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path,
                Extensions = { ["traceId"] = HttpContext.TraceIdentifier }
            };
            return BadRequest(problemDetails);
        }

        var query = new CheckDuplicateExpenseQuery(propertyId!.Value, amount!.Value, date!.Value);
        var result = await _mediator.Send(query);

        if (result.IsDuplicate)
        {
            _logger.LogInformation(
                "Potential duplicate expense detected: PropertyId={PropertyId}, Amount={Amount}, Date={Date} at {Timestamp}",
                propertyId,
                amount,
                date,
                DateTime.UtcNow);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get expense totals for a given year with per-property breakdown (AC-3.5.2, AC-3.5.4).
    /// </summary>
    /// <param name="year">Tax year (defaults to current year)</param>
    /// <returns>Total expenses and per-property breakdown</returns>
    /// <response code="200">Returns expense totals (returns $0 if no expenses)</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("expenses/totals")]
    [ProducesResponseType(typeof(ExpenseTotalsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpenseTotals([FromQuery] int? year = null)
    {
        var effectiveYear = year ?? DateTime.UtcNow.Year;
        var query = new GetExpenseTotalsQuery(effectiveYear);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved expense totals for year {Year}: Total={TotalExpenses}, Properties={PropertyCount} at {Timestamp}",
            effectiveYear,
            response.TotalExpenses,
            response.ByProperty.Count,
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Get all expenses across all properties with filtering and pagination (AC-3.4.1, AC-3.4.3, AC-3.4.4, AC-3.4.5, AC-3.4.8).
    /// </summary>
    /// <param name="dateFrom">Optional: Filter start date</param>
    /// <param name="dateTo">Optional: Filter end date</param>
    /// <param name="categoryIds">Optional: Filter by one or more category IDs (multi-select)</param>
    /// <param name="search">Optional: Search description text (case-insensitive, partial match)</param>
    /// <param name="year">Optional: Filter by tax year</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
    /// <returns>Paginated list of expenses</returns>
    /// <response code="200">Returns paginated expenses (empty list if no matches)</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(PagedResult<ExpenseListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllExpenses(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] List<Guid>? categoryIds = null,
        [FromQuery] string? search = null,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAllExpensesQuery(
            dateFrom,
            dateTo,
            categoryIds,
            search,
            year,
            page,
            pageSize);

        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} expenses (page {Page}/{TotalPages}, total {TotalCount}) at {Timestamp}",
            response.Items.Count,
            response.Page,
            response.TotalPages,
            response.TotalCount,
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Create a new expense (AC-3.1.1, AC-3.1.6).
    /// </summary>
    /// <param name="request">Expense details</param>
    /// <returns>The newly created expense's ID</returns>
    /// <response code="201">Returns the newly created expense ID</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property or category not found</response>
    [HttpPost("expenses")]
    [ProducesResponseType(typeof(CreateExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var command = new CreateExpenseCommand(
            request.PropertyId,
            request.Amount,
            request.Date,
            request.CategoryId,
            request.Description);

        // Validate command
        var validationResult = await _createValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var problemDetails = CreateValidationProblemDetails(validationResult);
            return BadRequest(problemDetails);
        }

        var expenseId = await _mediator.Send(command);

        _logger.LogInformation(
            "Expense created: {ExpenseId} for property {PropertyId}, amount {Amount} at {Timestamp}",
            expenseId,
            request.PropertyId,
            request.Amount,
            DateTime.UtcNow);

        var response = new CreateExpenseResponse(expenseId);

        return CreatedAtAction(
            nameof(CreateExpense),
            new { id = expenseId },
            response);
    }

    /// <summary>
    /// Get all expense categories (AC-3.1.4).
    /// </summary>
    /// <returns>List of expense categories (15 IRS Schedule E categories)</returns>
    /// <response code="200">Returns the list of expense categories</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("expense-categories")]
    [ProducesResponseType(typeof(ExpenseCategoriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetExpenseCategories()
    {
        var query = new GetExpenseCategoriesQuery();
        var categories = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} expense categories at {Timestamp}",
            categories.Count,
            DateTime.UtcNow);

        return Ok(new ExpenseCategoriesResponse(categories, categories.Count));
    }

    /// <summary>
    /// Get expenses for a property (AC-3.1.7).
    /// </summary>
    /// <param name="id">Property GUID</param>
    /// <param name="year">Optional tax year filter</param>
    /// <returns>List of expenses with YTD total</returns>
    /// <response code="200">Returns the list of expenses</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If property not found</response>
    [HttpGet("properties/{id:guid}/expenses")]
    [ProducesResponseType(typeof(ExpenseListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpensesByProperty(Guid id, [FromQuery] int? year = null)
    {
        var query = new GetExpensesByPropertyQuery(id, year);
        var response = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved {Count} expenses for property {PropertyId}, year {Year} at {Timestamp}",
            response.TotalCount,
            id,
            year?.ToString() ?? "all",
            DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Get a single expense by ID (AC-3.2.1, AC-3.2.2).
    /// </summary>
    /// <param name="id">Expense GUID</param>
    /// <returns>Expense details</returns>
    /// <response code="200">Returns the expense</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If expense not found</response>
    [HttpGet("expenses/{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpense(Guid id)
    {
        var query = new GetExpenseQuery(id);
        var expense = await _mediator.Send(query);

        _logger.LogInformation(
            "Retrieved expense {ExpenseId} at {Timestamp}",
            id,
            DateTime.UtcNow);

        return Ok(expense);
    }

    /// <summary>
    /// Update an existing expense (AC-3.2.1, AC-3.2.3, AC-3.2.4).
    /// PropertyId cannot be changed - delete and recreate to move expense to different property.
    /// </summary>
    /// <param name="id">Expense GUID</param>
    /// <param name="request">Updated expense details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Expense updated successfully</response>
    /// <response code="400">If validation fails</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If expense or category not found</response>
    [HttpPut("expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseRequest request)
    {
        var command = new UpdateExpenseCommand(
            id,
            request.Amount,
            request.Date,
            request.CategoryId,
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
            "Expense updated: {ExpenseId}, amount {Amount} at {Timestamp}",
            id,
            request.Amount,
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Delete an expense (soft delete) (AC-3.3.1, AC-3.3.3).
    /// Sets DeletedAt timestamp without physically removing the record.
    /// </summary>
    /// <param name="id">Expense GUID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Expense deleted successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If expense not found</response>
    [HttpDelete("expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var command = new DeleteExpenseCommand(id);
        await _mediator.Send(command);

        _logger.LogInformation(
            "Expense deleted: {ExpenseId} at {Timestamp}",
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
/// Request model for creating an expense.
/// </summary>
public record CreateExpenseRequest(
    Guid PropertyId,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string? Description
);

/// <summary>
/// Response model for successful expense creation.
/// </summary>
public record CreateExpenseResponse(
    Guid Id
);

/// <summary>
/// Response model for expense categories list.
/// </summary>
public record ExpenseCategoriesResponse(
    List<ExpenseCategoryDto> Items,
    int TotalCount
);

/// <summary>
/// Request model for updating an expense (AC-3.2.1).
/// Note: PropertyId is not included - expenses cannot be moved between properties.
/// </summary>
public record UpdateExpenseRequest(
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string? Description
);
