using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Application.Properties;

/// <summary>
/// Command for soft-deleting a property (AC-2.5.2).
/// Sets DeletedAt timestamp on property record without cascade to related entities.
/// </summary>
public record DeletePropertyCommand(Guid Id) : IRequest;

/// <summary>
/// Handler for DeletePropertyCommand.
/// Soft-deletes the property while preserving related expenses, income, and receipts for tax records (AC-2.5.2, AC-2.5.3).
/// </summary>
public class DeletePropertyCommandHandler : IRequestHandler<DeletePropertyCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeletePropertyCommandHandler> _logger;

    public DeletePropertyCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<DeletePropertyCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        // Find property with tenant isolation and soft-delete check
        var property = await _dbContext.Properties
            .Where(p => p.Id == request.Id && p.AccountId == _currentUser.AccountId && p.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            _logger.LogWarning(
                "Property not found for deletion: {PropertyId}, AccountId: {AccountId}",
                request.Id,
                _currentUser.AccountId);
            throw new NotFoundException("Property", request.Id);
        }

        // Soft delete - set DeletedAt timestamp (AC-2.5.2)
        // IMPORTANT: Do NOT cascade delete to Expenses, Income, or Receipts
        // They must be preserved for historical tax reporting (AC-2.5.3)
        property.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Property deleted: {PropertyId}, AccountId: {AccountId}, UserId: {UserId}",
            request.Id,
            _currentUser.AccountId,
            _currentUser.UserId);
    }
}
