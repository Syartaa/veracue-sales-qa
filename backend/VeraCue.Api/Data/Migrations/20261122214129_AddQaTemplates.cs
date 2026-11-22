using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeraCue.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaTemplates", x => x.Id);
                    table.CheckConstraint("CK_QaTemplates_Name_NotBlank", "length(btrim(\"Name\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "QaCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaCriteria", x => x.Id);
                    table.CheckConstraint("CK_QaCriteria_Instructions_NotBlank", "length(btrim(\"Instructions\")) > 0");
                    table.CheckConstraint("CK_QaCriteria_MaxPoints_Range", "\"MaxPoints\" BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_QaCriteria_Name_NotBlank", "length(btrim(\"Name\")) > 0");
                    table.CheckConstraint("CK_QaCriteria_SortOrder_NonNegative", "\"SortOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_QaCriteria_QaTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "QaTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaCriteria_TemplateId_SortOrder",
                table: "QaCriteria",
                columns: new[] { "TemplateId", "SortOrder" });

            // Hand-written: no two ACTIVE templates may share a name, ignoring case
            // ("Sales QA" and "sales qa" clash). Archived templates don't count, so a
            // name can be reused after archiving. EF Core can't model this index itself.
            migrationBuilder.Sql(
                """CREATE UNIQUE INDEX "IX_QaTemplates_Name_Active" ON "QaTemplates" (lower("Name")) WHERE "IsArchived" = false;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_QaTemplates_Name_Active";""");

            migrationBuilder.DropTable(
                name: "QaCriteria");

            migrationBuilder.DropTable(
                name: "QaTemplates");
        }
    }
}
