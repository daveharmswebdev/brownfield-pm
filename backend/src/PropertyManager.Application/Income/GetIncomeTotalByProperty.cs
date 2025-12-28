using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using PropertyEntity = PropertyManager.Domain.Entities.Property;

namespace PropertyManager.Application.Income;

/// <summary>
/// Query to get income total for a specific property and year (AC-4.1.4).
/// </summary>
public record GetIncomeTotalByPropertyQuery(
    Guid PropertyId,
    int Year
) : IRequest<decimal>;

/// <summary>
/// Handler for GetIncomeTotalByPropertyQuery.
/// Returns sum of income for a property in a given year.
/// Returns 0 if no income entries.
/// </summary>
public class GetIncomeTotalByPropertyQueryHandler : IRequestHandler<GetIncomeTotalByPropertyQuery, decimal>
{
    private readonly IAppDbContext _dbContext;

    public GetIncomeTotalByPropertyQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> Handle(GetIncomeTotalByPropertyQuery request, CancellationToken cancellationToken)
    {
        // Validate property exists (global query filter handles account isolation)
        var propertyExists = await _dbContext.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException(nameof(PropertyEntity), request.PropertyId);
        }

        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        var total = await _dbContext.Income
            .Where(i => i.PropertyId == request.PropertyId
                     && i.Date >= yearStart
                     && i.Date <= yearEnd)
            .SumAsync(i => i.Amount, cancellationToken);

        return total;
    }
}
