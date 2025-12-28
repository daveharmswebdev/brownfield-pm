using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Income;

/// <summary>
/// Query for retrieving a single income entry by ID (AC-4.2.2).
/// </summary>
public record GetIncomeByIdQuery(
    Guid Id
) : IRequest<IncomeDto>;

/// <summary>
/// Handler for GetIncomeByIdQuery.
/// Retrieves a single income entry with AccountId validation.
/// </summary>
public class GetIncomeByIdQueryHandler : IRequestHandler<GetIncomeByIdQuery, IncomeDto>
{
    private readonly IAppDbContext _dbContext;

    public GetIncomeByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IncomeDto> Handle(GetIncomeByIdQuery request, CancellationToken cancellationToken)
    {
        // Find income - AccountId filtering handled by global query filter
        var income = await _dbContext.Income
            .Where(i => i.Id == request.Id && i.DeletedAt == null)
            .Include(i => i.Property)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (income == null)
        {
            throw new NotFoundException(nameof(IncomeEntity), request.Id);
        }

        return income;
    }
}
