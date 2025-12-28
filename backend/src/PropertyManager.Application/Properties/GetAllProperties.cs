using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;

namespace PropertyManager.Application.Properties;

/// <summary>
/// Query to get all properties for the current user's account.
/// </summary>
/// <param name="Year">Optional tax year filter for expense/income totals (defaults to current year)</param>
public record GetAllPropertiesQuery(int? Year = null) : IRequest<GetAllPropertiesResponse>;

/// <summary>
/// Response containing list of properties.
/// </summary>
public record GetAllPropertiesResponse(
    IReadOnlyList<PropertySummaryDto> Items,
    int TotalCount
);

/// <summary>
/// Summary DTO for property list display (AC-2.1.4).
/// </summary>
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

/// <summary>
/// Handler for GetAllPropertiesQuery.
/// Returns all properties for the current user's account.
/// </summary>
public class GetAllPropertiesQueryHandler : IRequestHandler<GetAllPropertiesQuery, GetAllPropertiesResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetAllPropertiesQueryHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<GetAllPropertiesResponse> Handle(GetAllPropertiesQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year ?? DateTime.UtcNow.Year;

        var properties = await _dbContext.Properties
            .Where(p => p.AccountId == _currentUser.AccountId && p.DeletedAt == null)
            .OrderBy(p => p.Name)
            .Select(p => new PropertySummaryDto(
                p.Id,
                p.Name,
                p.Street,
                p.City,
                p.State,
                p.ZipCode,
                _dbContext.Expenses
                    .Where(e => e.PropertyId == p.Id
                        && e.AccountId == _currentUser.AccountId
                        && e.DeletedAt == null
                        && e.Date.Year == year)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                _dbContext.Income
                    .Where(i => i.PropertyId == p.Id
                        && i.AccountId == _currentUser.AccountId
                        && i.DeletedAt == null
                        && i.Date.Year == year)
                    .Sum(i => (decimal?)i.Amount) ?? 0m
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new GetAllPropertiesResponse(properties, properties.Count);
    }
}
