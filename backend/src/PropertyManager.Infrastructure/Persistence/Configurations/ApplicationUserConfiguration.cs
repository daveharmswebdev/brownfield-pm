using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Infrastructure.Identity;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // AccountId is required for multi-tenancy
        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_AspNetUsers_AccountId");

        // Role property for authorization (Owner | Contributor)
        builder.Property(e => e.Role)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Owner");

        // Custom audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Relationship to Account
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
