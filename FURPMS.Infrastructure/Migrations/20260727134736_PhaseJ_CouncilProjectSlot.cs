using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseJ_CouncilProjectSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "meeting_id",
                table: "council_project_assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_duration_minutes",
                table: "council_project_assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_order",
                table: "council_project_assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "slot_start_at",
                table: "council_project_assignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "meeting_id",
                table: "council_project_assignments");

            migrationBuilder.DropColumn(
                name: "slot_duration_minutes",
                table: "council_project_assignments");

            migrationBuilder.DropColumn(
                name: "slot_order",
                table: "council_project_assignments");

            migrationBuilder.DropColumn(
                name: "slot_start_at",
                table: "council_project_assignments");
        }
    }
}
