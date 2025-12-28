using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.ToTable("Income");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Income_AccountId");

        builder.Property(e => e.PropertyId)
            .IsRequired();

        builder.HasIndex(e => e.PropertyId)
            .HasDatabaseName("IX_Income_PropertyId");

        // Composite index for efficient property + date queries
        builder.HasIndex(e => new { e.PropertyId, e.Date })
            .HasDatabaseName("IX_Income_PropertyId_Date");

        builder.Property(e => e.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Source)
            .HasMaxLength(255);

        builder.Property(e => e.Description);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.DeletedAt);

        // Relationships
        builder.HasOne(e => e.Account)
            .WithMany(a => a.Income)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Income)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note: CreatedByUserId references ApplicationUser (Identity)
        // No navigation property - just the FK for data integrity
        builder.HasIndex(e => e.CreatedByUserId)
            .HasDatabaseName("IX_Income_CreatedByUserId");
    }
}
