using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain.Entities;
using IncomeEntity = PropertyManager.Domain.Entities.Income;

namespace PropertyManager.Application.Common.Interfaces;

/// <summary>
/// Interface for the application database context.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IAppDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Property> Properties { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<IncomeEntity> Income { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Invitation> Invitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
