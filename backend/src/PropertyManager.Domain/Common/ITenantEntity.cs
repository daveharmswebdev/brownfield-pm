namespace PropertyManager.Domain.Common;

/// <summary>
/// Marker interface for entities that belong to a tenant (Account).
/// Used by EF Core global query filter for multi-tenancy.
/// </summary>
public interface ITenantEntity
{
    Guid AccountId { get; set; }
}
