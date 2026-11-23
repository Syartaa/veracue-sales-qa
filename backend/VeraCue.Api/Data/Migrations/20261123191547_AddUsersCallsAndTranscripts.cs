using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraCue.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersCallsAndTranscripts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Email_LowerCase", "\"Email\" = lower(\"Email\")");
                });

            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    AudioStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FailedStage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AgentSpeaker = table.Column<short>(type: "smallint", nullable: true),
                    TranscriptionModel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProcessingPermissionConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.Id);
                    table.CheckConstraint("CK_Calls_DurationMs_Positive", "\"DurationMs\" IS NULL OR \"DurationMs\" > 0");
                    table.CheckConstraint("CK_Calls_Failed_HasStage", "\"Status\" <> 'Failed' OR \"FailedStage\" IS NOT NULL");
                    table.CheckConstraint("CK_Calls_FailedStage", "\"FailedStage\" IS NULL OR \"FailedStage\" IN ('Transcription', 'Evaluation')");
                    table.CheckConstraint("CK_Calls_FileSizeBytes_Positive", "\"FileSizeBytes\" > 0");
                    table.CheckConstraint("CK_Calls_Status", "\"Status\" IN ('Uploaded', 'Transcribing', 'Transcribed', 'Evaluating', 'ReadyForReview', 'Reviewed', 'Failed')");
                    table.CheckConstraint("CK_Calls_Title_NotBlank", "length(btrim(\"Title\")) > 0");
                    table.ForeignKey(
                        name: "FK_Calls_QaTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "QaTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Calls_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallId = table.Column<Guid>(type: "uuid", nullable: false),
                    SegmentIndex = table.Column<int>(type: "integer", nullable: false),
                    Speaker = table.Column<short>(type: "smallint", nullable: false),
                    StartMs = table.Column<int>(type: "integer", nullable: false),
                    EndMs = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptSegments", x => x.Id);
                    table.CheckConstraint("CK_TranscriptSegments_Confidence_Range", "\"Confidence\" IS NULL OR (\"Confidence\" >= 0 AND \"Confidence\" <= 1)");
                    table.CheckConstraint("CK_TranscriptSegments_EndMs_AfterStart", "\"EndMs\" >= \"StartMs\"");
                    table.CheckConstraint("CK_TranscriptSegments_SegmentIndex_NonNegative", "\"SegmentIndex\" >= 0");
                    table.CheckConstraint("CK_TranscriptSegments_Speaker_NonNegative", "\"Speaker\" >= 0");
                    table.CheckConstraint("CK_TranscriptSegments_StartMs_NonNegative", "\"StartMs\" >= 0");
                    table.ForeignKey(
                        name: "FK_TranscriptSegments_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_AudioStorageKey",
                table: "Calls",
                column: "AudioStorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calls_Status",
                table: "Calls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_TemplateId",
                table: "Calls",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_UploadedAt",
                table: "Calls",
                column: "UploadedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Calls_UploadedByUserId",
                table: "Calls",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_CallId_SegmentIndex",
                table: "TranscriptSegments",
                columns: new[] { "CallId", "SegmentIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptSegments");

            migrationBuilder.DropTable(
                name: "Calls");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
