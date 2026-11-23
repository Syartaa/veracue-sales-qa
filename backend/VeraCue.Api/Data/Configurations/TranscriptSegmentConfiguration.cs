using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for TranscriptSegments (see "VeraCue — Database Design").</summary>
public class TranscriptSegmentConfiguration : IEntityTypeConfiguration<TranscriptSegment>
{
    public void Configure(EntityTypeBuilder<TranscriptSegment> builder)
    {
        builder.ToTable("TranscriptSegments", table =>
        {
            table.HasCheckConstraint("CK_TranscriptSegments_SegmentIndex_NonNegative", "\"SegmentIndex\" >= 0");
            table.HasCheckConstraint("CK_TranscriptSegments_Speaker_NonNegative", "\"Speaker\" >= 0");
            table.HasCheckConstraint("CK_TranscriptSegments_StartMs_NonNegative", "\"StartMs\" >= 0");
            table.HasCheckConstraint("CK_TranscriptSegments_EndMs_AfterStart", "\"EndMs\" >= \"StartMs\"");
            table.HasCheckConstraint("CK_TranscriptSegments_Confidence_Range", "\"Confidence\" IS NULL OR (\"Confidence\" >= 0 AND \"Confidence\" <= 1)");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Text).IsRequired(); // text

        // Transcript in order, and a retry can never insert the same segment twice.
        builder.HasIndex(s => new { s.CallId, s.SegmentIndex }).IsUnique();
    }
}
