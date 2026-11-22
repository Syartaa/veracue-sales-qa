namespace VeraCue.Api.Services.Transcription;

/// <summary>
/// One normalized piece of the transcript — our own shape, independent of the provider.
/// Maps 1:1 to the TranscriptSegments table in Phase 3.
/// </summary>
/// <param name="Index">0-based position in the call; the AI cites evidence by this number.</param>
/// <param name="Speaker">Diarization speaker number (0, 1, ...). Not yet "agent" or "customer".</param>
public sealed record TranscriptSegment(
    int Index,
    int Speaker,
    int StartMs,
    int EndMs,
    string Text,
    double? Confidence);

public sealed record TranscriptionResult(
    string Model,
    int DurationMs,
    IReadOnlyList<TranscriptSegment> Segments);
