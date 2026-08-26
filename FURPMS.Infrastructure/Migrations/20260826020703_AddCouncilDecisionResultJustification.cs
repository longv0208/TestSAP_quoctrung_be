using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouncilDecisionResultJustification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "result_justification",
                table: "council_decisions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "result_justification",
                table: "council_decisions");
        }
    }
}
