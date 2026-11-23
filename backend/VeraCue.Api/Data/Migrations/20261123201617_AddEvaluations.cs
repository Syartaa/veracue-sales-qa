using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraCue.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateNameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                    table.CheckConstraint("CK_Evaluations_Reviewed_HasReviewer", "(\"ReviewedAt\" IS NULL) = (\"ReviewedByUserId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Evaluations_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Evaluations_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CriterionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    NameSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InstructionsSnapshot = table.Column<string>(type: "text", nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    AiOutcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AiScore = table.Column<int>(type: "integer", nullable: true),
                    AiExplanation = table.Column<string>(type: "text", nullable: false),
                    FinalOutcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FinalScore = table.Column<int>(type: "integer", nullable: true),
                    OverrideReason = table.Column<string>(type: "text", nullable: true),
                    OverriddenByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverriddenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionResults", x => x.Id);
                    table.CheckConstraint("CK_CriterionResults_AiOutcome", "\"AiOutcome\" IN ('Met', 'Partial', 'NotMet', 'NotApplicable', 'NeedsReview')");
                    table.CheckConstraint("CK_CriterionResults_AiScore_MatchesOutcome", "(\"AiOutcome\" IN ('NotApplicable', 'NeedsReview')) = (\"AiScore\" IS NULL)");
                    table.CheckConstraint("CK_CriterionResults_AiScore_Range", "\"AiScore\" IS NULL OR (\"AiScore\" >= 0 AND \"AiScore\" <= \"MaxPoints\")");
                    table.CheckConstraint("CK_CriterionResults_FinalOutcome", "\"FinalOutcome\" IS NULL OR \"FinalOutcome\" IN ('Met', 'Partial', 'NotMet', 'NotApplicable', 'NeedsReview')");
                    table.CheckConstraint("CK_CriterionResults_FinalOutcome_IsDecision", "\"FinalOutcome\" IS NULL OR \"FinalOutcome\" <> 'NeedsReview'");
                    table.CheckConstraint("CK_CriterionResults_FinalScore_MatchesOutcome", "(\"FinalOutcome\" IS NULL OR \"FinalOutcome\" = 'NotApplicable') = (\"FinalScore\" IS NULL)");
                    table.CheckConstraint("CK_CriterionResults_FinalScore_Range", "\"FinalScore\" IS NULL OR (\"FinalScore\" >= 0 AND \"FinalScore\" <= \"MaxPoints\")");
                    table.CheckConstraint("CK_CriterionResults_MaxPoints_Positive", "\"MaxPoints\" > 0");
                    table.CheckConstraint("CK_CriterionResults_Override_AllOrNothing", "(\"FinalOutcome\" IS NULL AND \"OverrideReason\" IS NULL AND \"OverriddenByUserId\" IS NULL AND \"OverriddenAt\" IS NULL) OR (\"FinalOutcome\" IS NOT NULL AND \"OverrideReason\" IS NOT NULL AND length(btrim(\"OverrideReason\")) > 0 AND \"OverriddenByUserId\" IS NOT NULL AND \"OverriddenAt\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CriterionResults_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriterionResults_QaCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "QaCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CriterionResults_Users_OverriddenByUserId",
                        column: x => x.OverriddenByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultEvidence",
                columns: table => new
                {
                    CriterionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    TranscriptSegmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultEvidence", x => new { x.CriterionResultId, x.TranscriptSegmentId });
                    table.ForeignKey(
                        name: "FK_ResultEvidence_CriterionResults_CriterionResultId",
                        column: x => x.CriterionResultId,
                        principalTable: "CriterionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultEvidence_TranscriptSegments_TranscriptSegmentId",
                        column: x => x.TranscriptSegmentId,
                        principalTable: "TranscriptSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionResults_CriterionId",
                table: "CriterionResults",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionResults_EvaluationId_CriterionId",
                table: "CriterionResults",
                columns: new[] { "EvaluationId", "CriterionId" },
                unique: true,
                filter: "\"CriterionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionResults_EvaluationId_SortOrder",
                table: "CriterionResults",
                columns: new[] { "EvaluationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionResults_OverriddenByUserId",
                table: "CriterionResults",
                column: "OverriddenByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_CallId",
                table: "Evaluations",
                column: "CallId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_ReviewedByUserId",
                table: "Evaluations",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEvidence_TranscriptSegmentId",
                table: "ResultEvidence",
                column: "TranscriptSegmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultEvidence");

            migrationBuilder.DropTable(
                name: "CriterionResults");

            migrationBuilder.DropTable(
                name: "Evaluations");
        }
    }
}
