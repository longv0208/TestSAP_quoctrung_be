using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewRoundScoringDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "scoring_deadline",
                table: "review_rounds",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scoring_deadline",
                table: "review_rounds");
        }
    }
}
