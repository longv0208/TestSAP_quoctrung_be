using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseO_Dieu15BudgetCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "max_percentage",
                table: "budget_expense_categories",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_percentage",
                table: "budget_expense_categories");
        }
    }
}
