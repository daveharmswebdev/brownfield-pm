namespace PropertyManager.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing current user context.
/// Used by AppDbContext for tenant filtering.
/// Implementation in Infrastructure layer.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid AccountId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
