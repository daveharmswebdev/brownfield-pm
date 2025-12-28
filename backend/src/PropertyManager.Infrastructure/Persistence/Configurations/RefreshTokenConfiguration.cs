using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.Property(e => e.TokenHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.RevokedAt);

        builder.Property(e => e.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Index for efficient token lookup by user
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        // Index for efficient lookup by account (tenant isolation)
        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_RefreshTokens_AccountId");

        // Index for token hash lookup during refresh
        builder.HasIndex(e => e.TokenHash)
            .HasDatabaseName("IX_RefreshTokens_TokenHash");
    }
}
