namespace VeraCue.Api.Entities;

/// <summary>
/// Links a criterion result to a transcript segment that supports it (table: ResultEvidence).
/// Because it's a foreign key, the database itself refuses evidence that points nowhere.
/// The segment's time and text are read through the link, never copied.
/// </summary>
public class ResultEvidence
{
    public Guid CriterionResultId { get; set; }
    public CriterionResult CriterionResult { get; set; } = null!;

    public Guid TranscriptSegmentId { get; set; }
    public TranscriptSegment TranscriptSegment { get; set; } = null!;
}
