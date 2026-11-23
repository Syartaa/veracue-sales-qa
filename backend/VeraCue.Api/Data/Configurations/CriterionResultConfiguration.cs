using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for CriterionResults (see "VeraCue — Database Design", CriterionResults).</summary>
public class CriterionResultConfiguration : IEntityTypeConfiguration<CriterionResult>
{
    public void Configure(EntityTypeBuilder<CriterionResult> builder)
    {
        var outcomes = string.Join(", ", Enum.GetNames<CriterionOutcome>().Select(o => $"'{o}'"));

        builder.ToTable("CriterionResults", table =>
        {
            table.HasCheckConstraint("CK_CriterionResults_MaxPoints_Positive", "\"MaxPoints\" > 0");
            table.HasCheckConstraint("CK_CriterionResults_AiOutcome", $"\"AiOutcome\" IN ({outcomes})");
            table.HasCheckConstraint("CK_CriterionResults_FinalOutcome",
                $"\"FinalOutcome\" IS NULL OR \"FinalOutcome\" IN ({outcomes})");

            // AI score: within 0..MaxPoints, and present exactly when the outcome is a scored one.
            table.HasCheckConstraint("CK_CriterionResults_AiScore_Range",
                "\"AiScore\" IS NULL OR (\"AiScore\" >= 0 AND \"AiScore\" <= \"MaxPoints\")");
            table.HasCheckConstraint("CK_CriterionResults_AiScore_MatchesOutcome",
                "(\"AiOutcome\" IN ('NotApplicable', 'NeedsReview')) = (\"AiScore\" IS NULL)");

            // Manager score: same range; null only when there is no override or it's NotApplicable.
            // An override is a decision, so it can't be "NeedsReview" again.
            table.HasCheckConstraint("CK_CriterionResults_FinalScore_Range",
                "\"FinalScore\" IS NULL OR (\"FinalScore\" >= 0 AND \"FinalScore\" <= \"MaxPoints\")");
            table.HasCheckConstraint("CK_CriterionResults_FinalScore_MatchesOutcome",
                "(\"FinalOutcome\" IS NULL OR \"FinalOutcome\" = 'NotApplicable') = (\"FinalScore\" IS NULL)");
            table.HasCheckConstraint("CK_CriterionResults_FinalOutcome_IsDecision",
                "\"FinalOutcome\" IS NULL OR \"FinalOutcome\" <> 'NeedsReview'");

            // All override columns are set together (with a non-blank reason) or all left empty.
            // Note "OverrideReason IS NOT NULL": length(btrim(NULL)) > 0 is NULL ("unknown"), and a
            // CHECK only rejects FALSE, so without it a missing reason would slip through.
            table.HasCheckConstraint("CK_CriterionResults_Override_AllOrNothing",
                "(\"FinalOutcome\" IS NULL AND \"OverrideReason\" IS NULL AND \"OverriddenByUserId\" IS NULL AND \"OverriddenAt\" IS NULL) OR " +
                "(\"FinalOutcome\" IS NOT NULL AND \"OverrideReason\" IS NOT NULL AND length(btrim(\"OverrideReason\")) > 0 " +
                "AND \"OverriddenByUserId\" IS NOT NULL AND \"OverriddenAt\" IS NOT NULL)");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.NameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(r => r.InstructionsSnapshot).IsRequired();
        builder.Property(r => r.AiExplanation).IsRequired();
        builder.Property(r => r.AiOutcome).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.FinalOutcome).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Version).IsRowVersion(); // xmin

        // Keep the result readable if the live criterion is deleted from the template later.
        builder.HasOne(r => r.Criterion).WithMany()
            .HasForeignKey(r => r.CriterionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.OverriddenBy).WithMany()
            .HasForeignKey(r => r.OverriddenByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Evidence).WithOne(e => e.CriterionResult)
            .HasForeignKey(e => e.CriterionResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.EvaluationId, r.SortOrder }); // scorecard in order
        // One criterion can't be scored twice in the same evaluation.
        builder.HasIndex(r => new { r.EvaluationId, r.CriterionId })
            .IsUnique()
            .HasFilter("\"CriterionId\" IS NOT NULL");
    }
}
