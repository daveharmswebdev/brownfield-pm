namespace PropertyManager.Domain.Common;

/// <summary>
/// Marker interface for entities supporting soft delete.
/// Applied to Properties, Expenses, Income, Receipts.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
