using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for ResultEvidence — the many-to-many link between results and segments.</summary>
public class ResultEvidenceConfiguration : IEntityTypeConfiguration<ResultEvidence>
{
    public void Configure(EntityTypeBuilder<ResultEvidence> builder)
    {
        builder.ToTable("ResultEvidence");

        // Composite key: the same segment can't be linked twice to one result.
        builder.HasKey(e => new { e.CriterionResultId, e.TranscriptSegmentId });

        builder.HasOne(e => e.TranscriptSegment).WithMany()
            .HasForeignKey(e => e.TranscriptSegmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Highlighting: "which criteria cite this segment?"
        builder.HasIndex(e => e.TranscriptSegmentId);
    }
}
