using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Scoring;

/// <summary>One criterion result as the calculator needs it.</summary>
public sealed record ScoreInput(
    int MaxPoints,
    CriterionOutcome AiOutcome,
    int? AiScore,
    CriterionOutcome? FinalOutcome = null,
    int? FinalScore = null);

/// <summary>Points achieved out of points available, and the rounded percentage (null when nothing is scored).</summary>
public sealed record ScoreTotal(int Achieved, int Available, int? Percent);

public sealed record CallScore(
    ScoreTotal Ai,
    ScoreTotal Current,
    int PendingPoints,
    int PendingCount,
    int OverrideCount,
    int MetCount,
    int PartialCount,
    int NotMetCount,
    int NotApplicableCount)
{
    /// <summary>"Mark as reviewed" is allowed only when no criterion is still waiting for a decision.</summary>
    public bool CanMarkReviewed => PendingCount == 0;
}

/// <summary>
/// Calculates call scores in the backend — never by the AI model (UI spec, "Scoring &amp; review rules"):
///   Score = sum of achieved points ÷ sum of max points of scored, applicable criteria × 100
/// - Not applicable: left out of both sums.
/// - Needs review: never counted as a pass; its points are reported separately as "pending".
/// - Overrides replace the AI score in the CURRENT score; the ORIGINAL AI score never changes.
/// </summary>
public static class ScoreCalculator
{
    public static CallScore Calculate(IReadOnlyCollection<ScoreInput> results)
    {
        // Original AI score: only the AI's own judgments, ignoring every override.
        var ai = Total(results.Select(r => (r.AiOutcome, r.AiScore, r.MaxPoints)));

        // Current score: the manager's decision where there is one, otherwise the AI's.
        var effective = results
            .Select(r => r.FinalOutcome is { } final
                ? (Outcome: final, Score: r.FinalScore, r.MaxPoints)
                : (Outcome: r.AiOutcome, Score: r.AiScore, r.MaxPoints))
            .ToList();
        var current = Total(effective);

        var pending = effective.Where(e => e.Outcome == CriterionOutcome.NeedsReview).ToList();

        return new CallScore(
            ai,
            current,
            PendingPoints: pending.Sum(e => e.MaxPoints),
            PendingCount: pending.Count,
            OverrideCount: results.Count(r => r.FinalOutcome is not null),
            MetCount: effective.Count(e => e.Outcome == CriterionOutcome.Met),
            PartialCount: effective.Count(e => e.Outcome == CriterionOutcome.Partial),
            NotMetCount: effective.Count(e => e.Outcome == CriterionOutcome.NotMet),
            NotApplicableCount: effective.Count(e => e.Outcome == CriterionOutcome.NotApplicable));
    }

    /// <summary>Is this outcome part of the score? Met, Partial and NotMet are; NotApplicable and NeedsReview aren't.</summary>
    public static bool IsScored(CriterionOutcome outcome) =>
        outcome is CriterionOutcome.Met or CriterionOutcome.Partial or CriterionOutcome.NotMet;

    private static ScoreTotal Total(IEnumerable<(CriterionOutcome Outcome, int? Score, int MaxPoints)> items)
    {
        var scored = items.Where(i => IsScored(i.Outcome)).ToList();
        var achieved = scored.Sum(i => i.Score ?? 0);
        var available = scored.Sum(i => i.MaxPoints);

        // No scored criteria (e.g. everything N/A or pending) means "no score yet", not 0% or 100%.
        int? percent = available == 0
            ? null
            : (int)Math.Round(achieved * 100.0 / available, MidpointRounding.AwayFromZero);

        return new ScoreTotal(achieved, available, percent);
    }
}
