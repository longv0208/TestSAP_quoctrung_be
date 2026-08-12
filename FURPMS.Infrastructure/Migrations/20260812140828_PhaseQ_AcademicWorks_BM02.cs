using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseQ_AcademicWorks_BM02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_works",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    venue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    authors = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    year = table.Column<int>(type: "int", nullable: true),
                    start_year = table.Column<int>(type: "int", nullable: true),
                    identifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    volume = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pages = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_works", x => x.id);
                    table.ForeignKey(
                        name: "fk_academic_works_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_works_user_id_work_type",
                table: "academic_works",
                columns: new[] { "user_id", "work_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "academic_works");
        }
    }
}
