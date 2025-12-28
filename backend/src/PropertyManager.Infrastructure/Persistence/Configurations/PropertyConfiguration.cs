using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Properties_AccountId");

        builder.Property(e => e.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Street)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.State)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(e => e.ZipCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.DeletedAt);

        // Relationship to Account
        builder.HasOne(e => e.Account)
            .WithMany(a => a.Properties)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
