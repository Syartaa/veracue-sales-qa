using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Evaluation;

/// <summary>
/// Checks an AI evaluation against the rules the AI cannot be trusted to follow on its own.
/// Never trusts the model: every criterion answered once, scores in range,
/// evidence pointing at segments that really exist in this call's transcript.
/// In Phase 4 these issues decide whether a result is saved or downgraded to NeedsReview.
/// </summary>
public static class AiResultValidator
{
    public static IReadOnlyList<string> Validate(
        AiEvaluation evaluation,
        IReadOnlyList<CriterionInput> criteria,
        int segmentCount)
    {
        var issues = new List<string>();
        var criteriaById = criteria.ToDictionary(c => c.CriterionId);

        foreach (var missing in criteria.Where(c => evaluation.Results.All(r => r.CriterionId != c.CriterionId)))
            issues.Add($"Missing result for criterion '{missing.CriterionId}'.");

        foreach (var duplicate in evaluation.Results.GroupBy(r => r.CriterionId).Where(g => g.Count() > 1))
            issues.Add($"Criterion '{duplicate.Key}' was answered {duplicate.Count()} times.");

        foreach (var result in evaluation.Results)
        {
            if (!criteriaById.TryGetValue(result.CriterionId, out var criterion))
            {
                issues.Add($"Unknown criterion '{result.CriterionId}' in the AI response.");
                continue;
            }

            var scored = result.Outcome is CriterionOutcome.Met or CriterionOutcome.Partial or CriterionOutcome.NotMet;

            if (scored && result.Score is null)
                issues.Add($"'{criterion.Name}': outcome {result.Outcome} but no score.");

            if (!scored && result.Score is not null)
                issues.Add($"'{criterion.Name}': outcome {result.Outcome} should not have a score.");

            if (result.Score is < 0 || result.Score > criterion.MaxPoints)
                issues.Add($"'{criterion.Name}': score {result.Score} is outside 0–{criterion.MaxPoints}.");

            var invalidEvidence = result.EvidenceSegments.Where(i => i < 0 || i >= segmentCount).ToList();
            if (invalidEvidence.Count > 0)
                issues.Add($"'{criterion.Name}': evidence cites segments that don't exist: {string.Join(", ", invalidEvidence)}.");

            var hasValidEvidence = result.EvidenceSegments.Any(i => i >= 0 && i < segmentCount);
            if (result.Outcome is CriterionOutcome.Met or CriterionOutcome.Partial && !hasValidEvidence)
                issues.Add($"'{criterion.Name}': {result.Outcome} without valid evidence (would become NeedsReview).");
        }

        return issues;
    }
}
