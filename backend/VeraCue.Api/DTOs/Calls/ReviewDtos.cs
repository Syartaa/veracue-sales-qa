using VeraCue.Api.Entities;

namespace VeraCue.Api.DTOs.Calls;

/// <summary>
/// Everything the Evaluation Review page needs, in one response:
/// the call, its transcript, the evaluation results with evidence, and the calculated scores.
/// Read-only: opening a review never calls the AI again.
/// </summary>
public sealed record CallReviewDto(
    CallDetailDto Call,
    short? AgentSpeaker,
    IReadOnlyList<TranscriptSegmentDto> Transcript,
    EvaluationDto? Evaluation);

public sealed record TranscriptSegmentDto(
    Guid Id,
    int Index,
    short Speaker,
    int StartMs,
    int EndMs,
    string Text,
    float? Confidence);

public sealed record EvaluationDto(
    Guid Id,
    string TemplateName,
    string ModelName,
    string PromptVersion,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewedByName,
    ScoreDto Score,
    IReadOnlyList<CriterionResultDto> Results);

/// <summary>Calculated by ScoreCalculator from the results; never stored.</summary>
public sealed record ScoreDto(
    int AiAchieved,
    int AiAvailable,
    int? AiPercent,
    int CurrentAchieved,
    int CurrentAvailable,
    int? CurrentPercent,
    int PendingPoints,
    int PendingCount,
    int OverrideCount,
    int MetCount,
    int PartialCount,
    int NotMetCount,
    int NotApplicableCount,
    bool CanMarkReviewed);

public sealed record CriterionResultDto(
    Guid Id,
    int SortOrder,
    string Name,
    string Instructions,
    int MaxPoints,
    CriterionOutcome AiOutcome,
    int? AiScore,
    string AiExplanation,
    CriterionOutcome? FinalOutcome,
    int? FinalScore,
    string? OverrideReason,
    string? OverriddenByName,
    DateTime? OverriddenAt,
    IReadOnlyList<EvidenceDto> Evidence,
    uint Version); // concurrency token: sent back with an override so a stale page can't overwrite a newer one

/// <summary>
/// Body of PUT /api/calls/{id}/results/{resultId}/override.
/// Nullable so missing values reach OverrideValidator and get friendly, field-level messages.
/// </summary>
public sealed record OverrideRequest(CriterionOutcome? Outcome, int? Score, string? Reason, uint? Version);

/// <summary>A cited transcript segment: enough to show "03:12 · Agent" and to seek the audio.</summary>
public sealed record EvidenceDto(Guid SegmentId, int SegmentIndex, int StartMs, short Speaker);
