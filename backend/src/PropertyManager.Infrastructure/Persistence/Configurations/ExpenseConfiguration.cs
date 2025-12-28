using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Expenses_AccountId");

        builder.Property(e => e.PropertyId)
            .IsRequired();

        builder.HasIndex(e => e.PropertyId)
            .HasDatabaseName("IX_Expenses_PropertyId");

        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Description);

        builder.Property(e => e.ReceiptId);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.DeletedAt);

        // Relationships
        builder.HasOne(e => e.Account)
            .WithMany(a => a.Expenses)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Expenses)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Receipt)
            .WithOne(r => r.Expense)
            .HasForeignKey<Expense>(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Note: CreatedByUserId references ApplicationUser (Identity)
        // No navigation property - just the FK for data integrity
        builder.HasIndex(e => e.CreatedByUserId)
            .HasDatabaseName("IX_Expenses_CreatedByUserId");

        // Performance indexes for common query patterns
        builder.HasIndex(e => new { e.AccountId, e.Date })
            .HasDatabaseName("IX_Expenses_AccountId_Date");

        builder.HasIndex(e => new { e.AccountId, e.PropertyId })
            .HasDatabaseName("IX_Expenses_AccountId_PropertyId");

        builder.HasIndex(e => new { e.AccountId, e.CategoryId })
            .HasDatabaseName("IX_Expenses_AccountId_CategoryId");
    }
}
