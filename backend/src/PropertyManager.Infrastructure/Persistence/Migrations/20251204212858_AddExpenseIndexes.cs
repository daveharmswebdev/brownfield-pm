using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId_CategoryId",
                table: "Expenses",
                columns: new[] { "AccountId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId_Date",
                table: "Expenses",
                columns: new[] { "AccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId_PropertyId",
                table: "Expenses",
                columns: new[] { "AccountId", "PropertyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expenses_AccountId_CategoryId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_AccountId_Date",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_AccountId_PropertyId",
                table: "Expenses");
        }
    }
}
