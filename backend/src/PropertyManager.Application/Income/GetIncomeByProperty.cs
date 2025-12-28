using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using PropertyEntity = PropertyManager.Domain.Entities.Property;

namespace PropertyManager.Application.Income;

/// <summary>
/// Query to get income for a specific property (AC-4.1.2, AC-4.1.6).
/// </summary>
public record GetIncomeByPropertyQuery(
    Guid PropertyId,
    int? Year = null
) : IRequest<IncomeListDto>;

/// <summary>
/// Response DTO for income by property.
/// </summary>
public record IncomeListDto(
    List<IncomeDto> Items,
    int TotalCount,
    decimal YtdTotal
);

/// <summary>
/// Handler for GetIncomeByPropertyQuery.
/// Returns income for a property, ordered by Date descending (newest first).
/// Optionally filtered by year.
/// </summary>
public class GetIncomeByPropertyQueryHandler : IRequestHandler<GetIncomeByPropertyQuery, IncomeListDto>
{
    private readonly IAppDbContext _dbContext;

    public GetIncomeByPropertyQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IncomeListDto> Handle(GetIncomeByPropertyQuery request, CancellationToken cancellationToken)
    {
        // Validate property exists (global query filter handles account isolation)
        var propertyExists = await _dbContext.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException(nameof(PropertyEntity), request.PropertyId);
        }

        var query = _dbContext.Income
            .Where(i => i.PropertyId == request.PropertyId);

        // Apply year filter if specified
        if (request.Year.HasValue)
        {
            var startDate = new DateOnly(request.Year.Value, 1, 1);
            var endDate = new DateOnly(request.Year.Value, 12, 31);
            query = query.Where(i => i.Date >= startDate && i.Date <= endDate);
        }

        var incomeList = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new IncomeDto(
                i.Id,
                i.PropertyId,
                i.Property.Name,
                i.Amount,
                i.Date,
                i.Source,
                i.Description,
                i.CreatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ytdTotal = incomeList.Sum(i => i.Amount);

        return new IncomeListDto(
            Items: incomeList,
            TotalCount: incomeList.Count,
            YtdTotal: ytdTotal
        );
    }
}
