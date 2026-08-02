using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseE_FixDocumentUploaderFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_users_uploaded_by_user_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_documents_uploaded_by_user_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "uploaded_by_user_id",
                table: "documents");

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by",
                table: "documents",
                column: "uploaded_by");

            migrationBuilder.AddForeignKey(
                name: "fk_documents_users_uploaded_by",
                table: "documents",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_users_uploaded_by",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_documents_uploaded_by",
                table: "documents");

            migrationBuilder.AddColumn<Guid>(
                name: "uploaded_by_user_id",
                table: "documents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by_user_id",
                table: "documents",
                column: "uploaded_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_documents_users_uploaded_by_user_id",
                table: "documents",
                column: "uploaded_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
