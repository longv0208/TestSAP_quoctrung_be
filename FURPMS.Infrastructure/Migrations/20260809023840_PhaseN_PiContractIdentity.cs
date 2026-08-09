using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseN_PiContractIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account_number",
                table: "academic_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "academic_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "academic_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "national_id_issued_date",
                table: "academic_profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id_issued_place",
                table: "academic_profiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_account_number",
                table: "academic_profiles");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "academic_profiles");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "academic_profiles");

            migrationBuilder.DropColumn(
                name: "national_id_issued_date",
                table: "academic_profiles");

            migrationBuilder.DropColumn(
                name: "national_id_issued_place",
                table: "academic_profiles");
        }
    }
}
