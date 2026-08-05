using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseM_ProgressReportFileUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "report_file_url",
                table: "progress_reports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "report_file_url",
                table: "progress_reports");
        }
    }
}
