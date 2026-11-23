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
