using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseL_RubricTemplateScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "applies_applied",
                table: "rubric_templates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "applies_basic",
                table: "rubric_templates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "rubric_template_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    cycle_id = table.Column<int>(type: "int", nullable: false),
                    track_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubric_template_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_rubric_template_scopes_rubric_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "rubric_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rubric_template_scopes_template_id_cycle_id_track_id",
                table: "rubric_template_scopes",
                columns: new[] { "template_id", "cycle_id", "track_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rubric_template_scopes");

            migrationBuilder.DropColumn(
                name: "applies_applied",
                table: "rubric_templates");

            migrationBuilder.DropColumn(
                name: "applies_basic",
                table: "rubric_templates");
        }
    }
}
