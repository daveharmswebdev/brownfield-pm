using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.TokenHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.InvitedByUserId)
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.AcceptedAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Index for efficient lookup by email
        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_Invitations_Email");

        // Index for efficient lookup by account (tenant isolation)
        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Invitations_AccountId");

        // Index for token hash lookup during registration
        builder.HasIndex(e => e.TokenHash)
            .HasDatabaseName("IX_Invitations_TokenHash");
    }
}
