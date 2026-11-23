using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Services.Scoring;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Reads a call's full review: transcript, AI results with evidence, and calculated scores.
/// (Phase 5 adds overrides and "Mark as reviewed" here.)
/// </summary>
public sealed class ReviewService(AppDbContext db, CallService calls)
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
                            .ToList()))
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

    public static ScoreDto ToDto(CallScore s) => new(
        s.Ai.Achieved, s.Ai.Available, s.Ai.Percent,
        s.Current.Achieved, s.Current.Available, s.Current.Percent,
        s.PendingPoints, s.PendingCount, s.OverrideCount,
        s.MetCount, s.PartialCount, s.NotMetCount, s.NotApplicableCount,
        s.CanMarkReviewed);
}
