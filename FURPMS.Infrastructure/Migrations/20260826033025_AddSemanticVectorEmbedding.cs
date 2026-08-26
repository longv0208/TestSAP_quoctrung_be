using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSemanticVectorEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dimensions",
                table: "semantic_search_vectors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "embedding",
                table: "semantic_search_vectors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_used",
                table: "semantic_search_vectors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dimensions",
                table: "semantic_search_vectors");

            migrationBuilder.DropColumn(
                name: "embedding",
                table: "semantic_search_vectors");

            migrationBuilder.DropColumn(
                name: "model_used",
                table: "semantic_search_vectors");
        }
    }
}
