using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Entities;
using VeraCue.Api.Services.Scoring;

namespace VeraCue.Api.Services.Calls;

/// <summary>Outcome of a manager action; the controller turns it into an HTTP response.</summary>
public sealed record ReviewActionResult(
    ReviewActionStatus Status,
    CallReviewDto? Review = null,
    Dictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static ReviewActionResult Ok(CallReviewDto review) => new(ReviewActionStatus.Ok, review);
    public static ReviewActionResult NotFound() => new(ReviewActionStatus.NotFound);
    public static ReviewActionResult Invalid(Dictionary<string, string[]> errors) => new(ReviewActionStatus.Invalid, Errors: errors);
    public static ReviewActionResult Locked(string message) => new(ReviewActionStatus.Conflict, Message: message);
}

public enum ReviewActionStatus { Ok, NotFound, Invalid, Conflict }

/// <summary>
/// A call's review: reading it (transcript, AI results with evidence, scores) and the
/// manager's actions on it (override, undo override, mark as reviewed).
/// </summary>
public sealed class ReviewService(AppDbContext db, CallService calls, TimeProvider time)
{
    public async Task<CallReviewDto?> GetReviewAsync(Guid callId, CancellationToken ct)
    {
        var call = await calls.GetAsync(callId, ct);
        if (call is null) return null;

        var agentSpeaker = await db.Calls.Where(c => c.Id == callId).Select(c => c.AgentSpeaker).FirstAsync(ct);

        var transcript = await db.TranscriptSegments.AsNoTracking()
            .Where(s => s.CallId == callId)
            .OrderBy(s => s.SegmentIndex)
            .Select(s => new TranscriptSegmentDto(s.Id, s.SegmentIndex, s.Speaker, s.StartMs, s.EndMs, s.Text, s.Confidence))
            .ToListAsync(ct);

        var evaluation = await db.Evaluations.AsNoTracking()
            .Where(e => e.CallId == callId)
            .Select(e => new
            {
                e.Id,
                e.TemplateNameSnapshot,
                e.ModelName,
                e.PromptVersion,
                e.CreatedAt,
                e.ReviewedAt,
                ReviewedByName = e.ReviewedBy != null ? e.ReviewedBy.DisplayName : null,
                Results = e.Results
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new CriterionResultDto(
                        r.Id,
                        r.SortOrder,
                        r.NameSnapshot,
                        r.InstructionsSnapshot,
                        r.MaxPoints,
                        r.AiOutcome,
                        r.AiScore,
                        r.AiExplanation,
                        r.FinalOutcome,
                        r.FinalScore,
                        r.OverrideReason,
                        r.OverriddenBy != null ? r.OverriddenBy.DisplayName : null,
                        r.OverriddenAt,
                        r.Evidence
                            .OrderBy(ev => ev.TranscriptSegment.SegmentIndex)
                            .Select(ev => new EvidenceDto(
                                ev.TranscriptSegmentId,
                                ev.TranscriptSegment.SegmentIndex,
                                ev.TranscriptSegment.StartMs,
                                ev.TranscriptSegment.Speaker))
                            .ToList(),
                        r.Version))
                    .ToList(),
            })
            .AsSplitQuery() // one query per collection, instead of one huge joined result
            .FirstOrDefaultAsync(ct);

        EvaluationDto? evaluationDto = null;
        if (evaluation is not null)
        {
            var score = ScoreCalculator.Calculate(evaluation.Results
                .Select(r => new ScoreInput(r.MaxPoints, r.AiOutcome, r.AiScore, r.FinalOutcome, r.FinalScore))
                .ToList());

            evaluationDto = new EvaluationDto(
                evaluation.Id,
                evaluation.TemplateNameSnapshot,
                evaluation.ModelName,
                evaluation.PromptVersion,
                evaluation.CreatedAt,
                evaluation.ReviewedAt,
                evaluation.ReviewedByName,
                ToDto(score),
                evaluation.Results);
        }

        return new CallReviewDto(call, agentSpeaker, transcript, evaluationDto);
    }

    // =====================================================================
    // Manager actions
    // =====================================================================

    /// <summary>
    /// Saves a manager's decision for one criterion. Only the Final*/Override* columns change;
    /// the AI's outcome, score and explanation stay exactly as they were.
    /// </summary>
    public async Task<ReviewActionResult> OverrideAsync(
        Guid callId, Guid resultId, OverrideRequest request, Guid userId, CancellationToken ct)
    {
        var result = await LoadResultAsync(callId, resultId, ct);
        if (result is null) return ReviewActionResult.NotFound();
        if (LockedMessage(result.Evaluation.Call) is { } locked) return ReviewActionResult.Locked(locked);

        var errors = OverrideValidator.Validate(request, result.MaxPoints);
        if (errors.Count > 0) return ReviewActionResult.Invalid(errors);

        ExpectVersion(result, request.Version);
        result.FinalOutcome = request.Outcome;
        result.FinalScore = request.Outcome == CriterionOutcome.NotApplicable ? null : request.Score;
        result.OverrideReason = request.Reason!.Trim();
        result.OverriddenByUserId = userId;
        result.OverriddenAt = Now;
        result.Evaluation.Call.UpdatedAt = Now;

        return await SaveAndReloadAsync(callId, ct);
    }

    /// <summary>Removes a manager's override, so the AI's judgment counts again.</summary>
    public async Task<ReviewActionResult> ClearOverrideAsync(Guid callId, Guid resultId, uint? version, CancellationToken ct)
    {
        var result = await LoadResultAsync(callId, resultId, ct);
        if (result is null) return ReviewActionResult.NotFound();
        if (LockedMessage(result.Evaluation.Call) is { } locked) return ReviewActionResult.Locked(locked);

        ExpectVersion(result, version);
        result.FinalOutcome = null;
        result.FinalScore = null;
        result.OverrideReason = null;
        result.OverriddenByUserId = null;
        result.OverriddenAt = null;
        result.Evaluation.Call.UpdatedAt = Now;

        return await SaveAndReloadAsync(callId, ct);
    }

    /// <summary>
    /// "Mark as reviewed": allowed only when no criterion is still pending (NeedsReview without a
    /// decision). Confirms every result that has no override and locks the review.
    /// </summary>
    public async Task<ReviewActionResult> MarkReviewedAsync(Guid callId, Guid userId, CancellationToken ct)
    {
        var evaluation = await db.Evaluations
            .Include(e => e.Call)
            .Include(e => e.Results)
            .FirstOrDefaultAsync(e => e.CallId == callId, ct);
        if (evaluation is null) return ReviewActionResult.NotFound();
        if (LockedMessage(evaluation.Call) is { } locked) return ReviewActionResult.Locked(locked);

        var score = ScoreCalculator.Calculate(evaluation.Results
            .Select(r => new ScoreInput(r.MaxPoints, r.AiOutcome, r.AiScore, r.FinalOutcome, r.FinalScore))
            .ToList());
        if (!score.CanMarkReviewed)
        {
            var n = score.PendingCount;
            return ReviewActionResult.Locked($"Decide {n} pending {(n == 1 ? "criterion" : "criteria")} first.");
        }

        evaluation.ReviewedAt = Now;
        evaluation.ReviewedByUserId = userId;
        evaluation.Call.Status = CallStatus.Reviewed;
        evaluation.Call.UpdatedAt = Now;

        return await SaveAndReloadAsync(callId, ct);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private Task<CriterionResult?> LoadResultAsync(Guid callId, Guid resultId, CancellationToken ct) =>
        db.CriterionResults
            .Include(r => r.Evaluation).ThenInclude(e => e.Call)
            .FirstOrDefaultAsync(r => r.Id == resultId && r.Evaluation.CallId == callId, ct);

    /// <summary>Reviews can only change while the call is "Ready for review" (decision: reviewed calls are locked).</summary>
    private static string? LockedMessage(Call call) => call.Status switch
    {
        CallStatus.ReadyForReview => null,
        CallStatus.Reviewed => "This call has been marked as reviewed and can no longer be changed.",
        _ => "This call hasn't finished processing yet.",
    };

    /// <summary>
    /// Optimistic concurrency: tell EF which version the page was showing. If the row changed
    /// since (another tab saved first), SaveChanges throws instead of silently overwriting it.
    /// </summary>
    private void ExpectVersion(CriterionResult result, uint? version)
    {
        if (version is { } v) db.Entry(result).Property(r => r.Version).OriginalValue = v;
    }

    private async Task<ReviewActionResult> SaveAndReloadAsync(Guid callId, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ReviewActionResult.Locked("This result was changed in another tab or window. Reload the page to see the latest version.");
        }

        db.ChangeTracker.Clear();
        return ReviewActionResult.Ok((await GetReviewAsync(callId, ct))!);
    }

    public static ScoreDto ToDto(CallScore s) => new(
        s.Ai.Achieved, s.Ai.Available, s.Ai.Percent,
        s.Current.Achieved, s.Current.Available, s.Current.Percent,
        s.PendingPoints, s.PendingCount, s.OverrideCount,
        s.MetCount, s.PartialCount, s.NotMetCount, s.NotApplicableCount,
        s.CanMarkReviewed);
}
