using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseG_CouncilMemberOpinion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "council_member_opinions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    decision_id = table.Column<int>(type: "int", nullable: false),
                    member_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    academic_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    budget_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_member_opinions", x => x.id);
                    table.ForeignKey(
                        name: "fk_council_member_opinions_council_decisions_decision_id",
                        column: x => x.decision_id,
                        principalTable: "council_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_council_member_opinions_decision_id",
                table: "council_member_opinions",
                column: "decision_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "council_member_opinions");
        }
    }
}
