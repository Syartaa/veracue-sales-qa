namespace VeraCue.Api.Entities;

/// <summary>Processing stages of a call (project reference §5). Stored as text in the database.</summary>
public enum CallStatus
{
    Uploaded,
    Transcribing,
    Transcribed,
    Evaluating,
    ReadyForReview,
    Reviewed,
    Failed,
}

/// <summary>Which stage failed, so "Retry" restarts at the right place (no re-transcribing).</summary>
public enum ProcessingStage
{
    Transcription,
    Evaluation,
}

/// <summary>One uploaded recording and its processing state (table: Calls).</summary>
public class Call
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TemplateId { get; set; }
    public QaTemplate Template { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public User UploadedBy { get; set; } = null!;

    public required string Title { get; set; }

    // ----- The recording (bytes live on the storage disk, not in the database) -----
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }

    /// <summary>Private storage key, e.g. "calls/{id}/call.mp3". Never a public URL.</summary>
    public required string AudioStorageKey { get; set; }

    /// <summary>Filled after transcription.</summary>
    public int? DurationMs { get; set; }

    // ----- Processing -----
    public CallStatus Status { get; set; } = CallStatus.Uploaded;

    /// <summary>Set (with ErrorMessage) only when Status is Failed.</summary>
    public ProcessingStage? FailedStage { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Which diarization speaker number is the sales agent (set during evaluation).</summary>
    public short? AgentSpeaker { get; set; }

    /// <summary>e.g. "nova-3": which model produced the transcript.</summary>
    public string? TranscriptionModel { get; set; }

    /// <summary>When the manager confirmed they may process this recording with third-party AI services.</summary>
    public DateTime ProcessingPermissionConfirmedAt { get; set; }

    public DateTime UploadedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token, mapped to PostgreSQL's built-in xmin column. If two
    /// updates race (e.g. the worker and a "Retry" click), the second one fails instead of
    /// silently overwriting the first.
    /// </summary>
    public uint Version { get; set; }

    public List<TranscriptSegment> Segments { get; set; } = [];
}
