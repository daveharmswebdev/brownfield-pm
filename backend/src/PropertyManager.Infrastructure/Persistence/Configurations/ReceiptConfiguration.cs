using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.Property(e => e.PropertyId);

        builder.Property(e => e.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.OriginalFileName)
            .HasMaxLength(255);

        builder.Property(e => e.ContentType)
            .HasMaxLength(100);

        builder.Property(e => e.FileSizeBytes);

        builder.Property(e => e.ExpenseId);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.ProcessedAt);

        builder.Property(e => e.DeletedAt);

        // Relationships
        builder.HasOne(e => e.Account)
            .WithMany(a => a.Receipts)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Receipts)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Note: CreatedByUserId references ApplicationUser (Identity)
        // No navigation property - just the FK for data integrity
        builder.HasIndex(e => e.CreatedByUserId)
            .HasDatabaseName("IX_Receipts_CreatedByUserId");

        // Note: Expense relationship is configured in ExpenseConfiguration
    }
}
