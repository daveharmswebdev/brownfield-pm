using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Common;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;

// Note: AppDbContext implements IAppDbContext for Application layer dependency inversion

namespace PropertyManager.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext with ASP.NET Core Identity integration,
/// global query filters for soft delete and tenant isolation.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    private readonly ICurrentUser? _currentUser;

    // Properties for EF Core query filter parameterization
    private Guid? CurrentAccountId => _currentUser?.IsAuthenticated == true ? _currentUser.AccountId : null;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Income> Income => Set<Income>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure global query filters for tenant isolation (ITenantEntity)
        ConfigureTenantFilters(modelBuilder);

        // Configure global query filters for soft delete (ISoftDeletable)
        ConfigureSoftDeleteFilters(modelBuilder);
    }

    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to ApplicationUser (Identity user)
        // When CurrentAccountId is null (no user or unauthenticated), no filter is applied
        modelBuilder.Entity<ApplicationUser>()
            .HasQueryFilter(e => CurrentAccountId == null || e.AccountId == CurrentAccountId);

        // Apply tenant filter to Property (combined with soft delete)
        modelBuilder.Entity<Property>()
            .HasQueryFilter(e => (CurrentAccountId == null || e.AccountId == CurrentAccountId)
                                 && e.DeletedAt == null);

        // Apply tenant filter to Expense (combined with soft delete)
        modelBuilder.Entity<Expense>()
            .HasQueryFilter(e => (CurrentAccountId == null || e.AccountId == CurrentAccountId)
                                 && e.DeletedAt == null);

        // Apply tenant filter to Income (combined with soft delete)
        modelBuilder.Entity<Income>()
            .HasQueryFilter(e => (CurrentAccountId == null || e.AccountId == CurrentAccountId)
                                 && e.DeletedAt == null);

        // Apply tenant filter to Receipt (combined with soft delete)
        modelBuilder.Entity<Receipt>()
            .HasQueryFilter(e => (CurrentAccountId == null || e.AccountId == CurrentAccountId)
                                 && e.DeletedAt == null);

        // Apply tenant filter to RefreshToken (no soft delete, just tenant isolation)
        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(e => CurrentAccountId == null || e.AccountId == CurrentAccountId);
    }

    private void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        // Note: Soft delete filters are combined with tenant filters above
        // This method is kept for documentation purposes
        // Entities with ISoftDeletable: Property, Expense, Income, Receipt
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }

        // Handle Account entity (not AuditableEntity but has CreatedAt)
        foreach (var entry in ChangeTracker.Entries<Account>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
        }

        // Handle ApplicationUser entity (Identity user with custom audit fields)
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }
}
