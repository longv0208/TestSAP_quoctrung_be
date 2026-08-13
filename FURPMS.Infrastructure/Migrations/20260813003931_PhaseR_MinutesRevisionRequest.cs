using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseR_MinutesRevisionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "revision_request_note",
                table: "council_decisions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revision_requested_at",
                table: "council_decisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revision_requested_by",
                table: "council_decisions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "revision_request_note",
                table: "council_decisions");

            migrationBuilder.DropColumn(
                name: "revision_requested_at",
                table: "council_decisions");

            migrationBuilder.DropColumn(
                name: "revision_requested_by",
                table: "council_decisions");
        }
    }
}
