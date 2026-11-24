using VeraCue.Api.Entities;

namespace VeraCue.Api.DTOs.Calls;

/// <summary>
/// Form fields of POST /api/calls (multipart/form-data, because it carries a file).
/// Nullable so missing values reach CallUploadValidator and get friendly messages.
/// </summary>
public sealed class UploadCallRequest
{
    public IFormFile? File { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Title { get; set; }
    public bool PermissionConfirmed { get; set; }
}

/// <summary>Query string of GET /api/calls (Call History).</summary>
public sealed class CallListQuery
{
    public string? Search { get; set; }

    /// <summary>A CallStatus name, or "InProgress" for Uploaded/Transcribing/Transcribed/Evaluating.</summary>
    public string? Status { get; set; }

    public Guid? TemplateId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
}

/// <summary>One row of Call History.</summary>
public sealed record CallListItemDto(
    Guid Id,
    string Title,
    Guid TemplateId,
    string TemplateName,
    int? DurationMs,
    CallStatus Status,
    ProcessingStage? FailedStage,
    int? AiScorePercent,
    int? CurrentScorePercent,
    int OverrideCount,
    int PendingCount,
    DateTime UploadedAt);

public sealed record CallListDto(IReadOnlyList<CallListItemDto> Items, int Total, int Page, int PageSize);

/// <summary>The Overview page: summary cards and the most recent calls.</summary>
public sealed record OverviewDto(
    int CallsEvaluated,     // calls with AI results (ready for review or reviewed), all time
    int? AverageScore,      // average current score of REVIEWED calls only (null if none yet)
    int PendingReview,      // calls waiting for a manager ("Ready for review")
    int Templates,          // active QA templates
    IReadOnlyList<CallListItemDto> Recent);

/// <summary>A call and its processing state, for the Processing page (and later Call History).</summary>
public sealed record CallDetailDto(
    Guid Id,
    string Title,
    Guid TemplateId,
    string TemplateName,
    CallStatus Status,
    ProcessingStage? FailedStage,
    string? ErrorMessage,
    string OriginalFileName,
    long FileSizeBytes,
    int? DurationMs,
    int SegmentCount,
    int SpeakerCount,
    DateTime UploadedAt,
    DateTime UpdatedAt,
    int? AiScorePercent = null,      // original AI score in %, once evaluated (null if nothing scorable)
    int? CurrentScorePercent = null, // score after overrides, in %
    int PendingCount = 0);           // criteria still waiting for a manager's decision
