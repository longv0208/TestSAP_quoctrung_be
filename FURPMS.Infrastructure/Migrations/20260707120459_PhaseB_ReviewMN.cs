using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhaseB_ReviewMN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_review_councils_projects_project_id",
                table: "review_councils");

            migrationBuilder.DropForeignKey(
                name: "fk_review_rounds_projects_project_id",
                table: "review_rounds");

            migrationBuilder.DropIndex(
                name: "ix_reviewer_feedbacks_council_id_reviewer_member_id",
                table: "reviewer_feedbacks");

            migrationBuilder.DropIndex(
                name: "ix_review_rounds_project_id_round_number",
                table: "review_rounds");

            migrationBuilder.DropIndex(
                name: "ix_review_councils_project_id",
                table: "review_councils");

            migrationBuilder.DropIndex(
                name: "ix_proposal_review_scores_council_id_evaluator_member_id",
                table: "proposal_review_scores");

            migrationBuilder.DropIndex(
                name: "ix_council_decisions_council_id",
                table: "council_decisions");

            migrationBuilder.DropIndex(
                name: "ix_acceptance_evaluations_council_id_evaluator_member_id",
                table: "acceptance_evaluations");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "review_rounds");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "review_councils");

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "reviewer_feedbacks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "dimension",
                table: "review_rounds",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "cycle_track_id",
                table: "review_rounds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "proposal_review_scores",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "council_decisions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "acceptance_evaluations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "council_project_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    council_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_project_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_council_project_assignments_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_council_project_assignments_review_councils_council_id",
                        column: x => x.council_id,
                        principalTable: "review_councils",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_rounds",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    round_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_rounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_rounds_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_rounds_review_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "review_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_council_id_project_id_reviewer_member_id",
                table: "reviewer_feedbacks",
                columns: new[] { "council_id", "project_id", "reviewer_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_project_id",
                table: "reviewer_feedbacks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_rounds_cycle_track_id_round_number_dimension",
                table: "review_rounds",
                columns: new[] { "cycle_track_id", "round_number", "dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_council_id_project_id_evaluator_member_id",
                table: "proposal_review_scores",
                columns: new[] { "council_id", "project_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_project_id",
                table: "proposal_review_scores",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_council_id_project_id",
                table: "council_decisions",
                columns: new[] { "council_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_project_id",
                table: "council_decisions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_council_id_project_id_evaluator_member_id",
                table: "acceptance_evaluations",
                columns: new[] { "council_id", "project_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_project_id",
                table: "acceptance_evaluations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_project_assignments_council_id_project_id",
                table: "council_project_assignments",
                columns: new[] { "council_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_council_project_assignments_project_id",
                table: "council_project_assignments",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_rounds_project_id_round_id",
                table: "project_rounds",
                columns: new[] { "project_id", "round_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_rounds_round_id",
                table: "project_rounds",
                column: "round_id");

            migrationBuilder.AddForeignKey(
                name: "fk_acceptance_evaluations_projects_project_id",
                table: "acceptance_evaluations",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_decisions_projects_project_id",
                table: "council_decisions",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_review_scores_projects_project_id",
                table: "proposal_review_scores",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_review_rounds_cycle_tracks_cycle_track_id",
                table: "review_rounds",
                column: "cycle_track_id",
                principalTable: "cycle_tracks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_reviewer_feedbacks_projects_project_id",
                table: "reviewer_feedbacks",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_acceptance_evaluations_projects_project_id",
                table: "acceptance_evaluations");

            migrationBuilder.DropForeignKey(
                name: "fk_council_decisions_projects_project_id",
                table: "council_decisions");

            migrationBuilder.DropForeignKey(
                name: "fk_proposal_review_scores_projects_project_id",
                table: "proposal_review_scores");

            migrationBuilder.DropForeignKey(
                name: "fk_review_rounds_cycle_tracks_cycle_track_id",
                table: "review_rounds");

            migrationBuilder.DropForeignKey(
                name: "fk_reviewer_feedbacks_projects_project_id",
                table: "reviewer_feedbacks");

            migrationBuilder.DropTable(
                name: "council_project_assignments");

            migrationBuilder.DropTable(
                name: "project_rounds");

            migrationBuilder.DropIndex(
                name: "ix_reviewer_feedbacks_council_id_project_id_reviewer_member_id",
                table: "reviewer_feedbacks");

            migrationBuilder.DropIndex(
                name: "ix_reviewer_feedbacks_project_id",
                table: "reviewer_feedbacks");

            migrationBuilder.DropIndex(
                name: "ix_review_rounds_cycle_track_id_round_number_dimension",
                table: "review_rounds");

            migrationBuilder.DropIndex(
                name: "ix_proposal_review_scores_council_id_project_id_evaluator_member_id",
                table: "proposal_review_scores");

            migrationBuilder.DropIndex(
                name: "ix_proposal_review_scores_project_id",
                table: "proposal_review_scores");

            migrationBuilder.DropIndex(
                name: "ix_council_decisions_council_id_project_id",
                table: "council_decisions");

            migrationBuilder.DropIndex(
                name: "ix_council_decisions_project_id",
                table: "council_decisions");

            migrationBuilder.DropIndex(
                name: "ix_acceptance_evaluations_council_id_project_id_evaluator_member_id",
                table: "acceptance_evaluations");

            migrationBuilder.DropIndex(
                name: "ix_acceptance_evaluations_project_id",
                table: "acceptance_evaluations");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "reviewer_feedbacks");

            migrationBuilder.DropColumn(
                name: "cycle_track_id",
                table: "review_rounds");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "proposal_review_scores");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "council_decisions");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "acceptance_evaluations");

            migrationBuilder.AlterColumn<string>(
                name: "dimension",
                table: "review_rounds",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "review_rounds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "review_councils",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_council_id_reviewer_member_id",
                table: "reviewer_feedbacks",
                columns: new[] { "council_id", "reviewer_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_rounds_project_id_round_number",
                table: "review_rounds",
                columns: new[] { "project_id", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_councils_project_id",
                table: "review_councils",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_council_id_evaluator_member_id",
                table: "proposal_review_scores",
                columns: new[] { "council_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_council_id",
                table: "council_decisions",
                column: "council_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_council_id_evaluator_member_id",
                table: "acceptance_evaluations",
                columns: new[] { "council_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_review_councils_projects_project_id",
                table: "review_councils",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_review_rounds_projects_project_id",
                table: "review_rounds",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
