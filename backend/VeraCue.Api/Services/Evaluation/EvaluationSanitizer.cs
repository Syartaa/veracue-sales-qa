using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Evaluation;

/// <summary>One criterion's result after the backend's checks, ready to save.</summary>
/// <param name="Downgraded">True when the AI's answer broke a rule and the result was turned into NeedsReview.</param>
public sealed record SanitizedResult(
    int CriterionIndex,
    CriterionOutcome Outcome,
    int? Score,
    string Explanation,
    IReadOnlyList<int> EvidenceSegments,
    bool Downgraded);

/// <summary>
/// Never trust the model. Turns the AI's raw answer into results that obey every rule,
/// so the database only ever receives grounded, consistent judgments:
/// - one result per criterion, in template order (a missing one becomes NeedsReview);
/// - evidence must point at segments that really exist in THIS transcript (others are dropped);
/// - Met / Partial without any valid evidence becomes NeedsReview ("Insufficient evidence"),
///   instead of keeping a score nobody can verify;
/// - a missing or out-of-range score becomes NeedsReview;
/// - NotApplicable / NeedsReview never carry a score.
/// </summary>
public static class EvaluationSanitizer
{
    private const int MaxExplanationLength = 2000;

    public static (IReadOnlyList<SanitizedResult> Results, short? AgentSpeaker) Sanitize(
        AiEvaluation ai,
        IReadOnlyList<CriterionInput> criteria,
        int segmentCount,
        IReadOnlyCollection<int> speakers)
    {
        var results = new List<SanitizedResult>(criteria.Count);

        for (var i = 0; i < criteria.Count; i++)
        {
            var criterion = criteria[i];
            var answer = ai.Results.FirstOrDefault(r => r.CriterionId == criterion.CriterionId);

            if (answer is null)
            {
                results.Add(new(i, CriterionOutcome.NeedsReview, null,
                    "The AI returned no result for this criterion, so a manager needs to decide.", [], Downgraded: true));
                continue;
            }

            // Only evidence that exists in this transcript, each segment once, in call order.
            var evidence = answer.EvidenceSegments
                .Where(index => index >= 0 && index < segmentCount)
                .Distinct()
                .Order()
                .ToList();

            var explanation = Clean(answer.Explanation);

            switch (answer.Outcome)
            {
                case CriterionOutcome.Met or CriterionOutcome.Partial when evidence.Count == 0:
                    results.Add(new(i, CriterionOutcome.NeedsReview, null,
                        $"Insufficient evidence: the AI suggested {Describe(answer.Outcome)} but cited no transcript segment that supports it. AI explanation: {explanation}",
                        [], Downgraded: true));
                    break;

                case CriterionOutcome.Met or CriterionOutcome.Partial or CriterionOutcome.NotMet
                    when answer.Score is null || answer.Score < 0 || answer.Score > criterion.MaxPoints:
                    results.Add(new(i, CriterionOutcome.NeedsReview, null,
                        $"The AI gave an invalid score ({answer.Score?.ToString() ?? "none"} of {criterion.MaxPoints}). AI explanation: {explanation}",
                        evidence, Downgraded: true));
                    break;

                case CriterionOutcome.Met or CriterionOutcome.Partial or CriterionOutcome.NotMet:
                    results.Add(new(i, answer.Outcome, answer.Score, explanation, evidence, Downgraded: false));
                    break;

                default: // NotApplicable, NeedsReview: no score, by definition
                    results.Add(new(i, answer.Outcome, null, explanation, evidence, Downgraded: false));
                    break;
            }
        }

        // Accept the AI's choice of agent only if that speaker number actually occurs in the transcript.
        short? agentSpeaker = ai.AgentSpeaker is { } speaker && speakers.Contains(speaker) ? (short)speaker : null;

        return (results, agentSpeaker);
    }

    private static string Clean(string? text)
    {
        var trimmed = string.IsNullOrWhiteSpace(text) ? "No explanation was given." : text.Trim();
        return trimmed.Length <= MaxExplanationLength ? trimmed : trimmed[..MaxExplanationLength] + "…";
    }

    private static string Describe(CriterionOutcome outcome) => outcome == CriterionOutcome.Met ? "Met" : "Partial";
}
