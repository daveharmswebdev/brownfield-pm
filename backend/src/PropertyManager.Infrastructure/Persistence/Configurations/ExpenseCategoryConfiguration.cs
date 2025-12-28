using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Infrastructure.Persistence.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ScheduleELine)
            .HasMaxLength(50);

        builder.Property(e => e.SortOrder)
            .IsRequired();

        // Note: No AccountId - this is global data (seed data)

        // Seed the 15 IRS Schedule E categories
        builder.HasData(
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Advertising", ScheduleELine = "Line 5", SortOrder = 1 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Auto and Travel", ScheduleELine = "Line 6", SortOrder = 2 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Cleaning and Maintenance", ScheduleELine = "Line 7", SortOrder = 3 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Name = "Commissions", ScheduleELine = "Line 8", SortOrder = 4 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Name = "Insurance", ScheduleELine = "Line 9", SortOrder = 5 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111106"), Name = "Legal and Professional Fees", ScheduleELine = "Line 10", SortOrder = 6 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111107"), Name = "Management Fees", ScheduleELine = "Line 11", SortOrder = 7 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111108"), Name = "Mortgage Interest", ScheduleELine = "Line 12", SortOrder = 8 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111109"), Name = "Other Interest", ScheduleELine = "Line 13", SortOrder = 9 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111110"), Name = "Repairs", ScheduleELine = "Line 14", SortOrder = 10 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Supplies", ScheduleELine = "Line 15", SortOrder = 11 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), Name = "Taxes", ScheduleELine = "Line 16", SortOrder = 12 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), Name = "Utilities", ScheduleELine = "Line 17", SortOrder = 13 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111114"), Name = "Depreciation", ScheduleELine = "Line 18", SortOrder = 14 },
            new ExpenseCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111115"), Name = "Other", ScheduleELine = "Line 19", SortOrder = 15 }
        );
    }
}
