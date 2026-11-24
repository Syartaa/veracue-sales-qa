using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Calls;

/// <summary>
/// Rules for a manager's override (UI spec "Override score dialog" + Database Design):
/// - the outcome is a decision: Met, Partial, Not met or Not applicable (never "Needs review");
/// - Met / Partial / Not met need a whole-number score from 0 to the criterion's max points;
/// - Not applicable has no score (it's left out of the total);
/// - a reason is always required — it's shown with the manager's name and time.
/// Error keys match the dialog's fields: "outcome", "score", "reason".
/// </summary>
public static class OverrideValidator
{
    public const int ReasonMaxLength = 1000;

    public static Dictionary<string, string[]> Validate(OverrideRequest request, int maxPoints)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Outcome is null)
            errors["outcome"] = ["Choose an outcome."];
        else if (request.Outcome == CriterionOutcome.NeedsReview)
            errors["outcome"] = ["An override must be a decision: Met, Partial, Not met or Not applicable."];
        else if (request.Outcome == CriterionOutcome.NotApplicable)
        {
            if (request.Score is not null)
                errors["score"] = ["A criterion marked Not applicable has no score."];
        }
        else if (request.Score is null)
            errors["score"] = ["Enter a score."];
        else if (request.Score < 0 || request.Score > maxPoints)
            errors["score"] = [$"The score must be between 0 and {maxPoints}."];

        if (string.IsNullOrWhiteSpace(request.Reason))
            errors["reason"] = ["A reason is required for every override."];
        else if (request.Reason.Trim().Length > ReasonMaxLength)
            errors["reason"] = [$"The reason can be at most {ReasonMaxLength} characters."];

        return errors;
    }
}
