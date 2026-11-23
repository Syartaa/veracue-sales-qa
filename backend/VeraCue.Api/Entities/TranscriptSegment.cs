namespace VeraCue.Api.Entities;

/// <summary>
/// One timestamped piece of a call's transcript (table: TranscriptSegments).
/// Every AI judgment must point at rows in this table as its evidence.
/// </summary>
public class TranscriptSegment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CallId { get; set; }
    public Call Call { get; set; } = null!;

    /// <summary>0, 1, 2 … in call order. The AI cites evidence by this number.</summary>
    public int SegmentIndex { get; set; }

    /// <summary>Diarization speaker number; Calls.AgentSpeaker says which one is the agent.</summary>
    public short Speaker { get; set; }

    public int StartMs { get; set; }
    public int EndMs { get; set; }

    public required string Text { get; set; }

    /// <summary>0–1 from the transcription model; low values flag uncertain text.</summary>
    public float? Confidence { get; set; }
}
