using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    document_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_by_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_decisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_decisions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_decisions_users_decided_by",
                        column: x => x.decided_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_decisions_decided_by",
                table: "project_decisions",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "ix_project_decisions_project_id_decided_at",
                table: "project_decisions",
                columns: new[] { "project_id", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "ix_project_decisions_source_entity_type_source_entity_id_decis",
                table: "project_decisions",
                columns: new[] { "source_entity_type", "source_entity_id", "decision_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_decisions");
        }
    }
}
