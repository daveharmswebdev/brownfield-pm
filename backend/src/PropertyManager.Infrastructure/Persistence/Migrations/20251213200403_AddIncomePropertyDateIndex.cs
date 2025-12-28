using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomePropertyDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Income_PropertyId_Date",
                table: "Income",
                columns: new[] { "PropertyId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Income_PropertyId_Date",
                table: "Income");
        }
    }
}
