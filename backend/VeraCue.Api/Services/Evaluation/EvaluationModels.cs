using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Evaluation;

/// <summary>A criterion as sent to the AI (snapshot of a QaCriteria row).</summary>
public sealed record CriterionInput(string CriterionId, string Name, string Instructions, int MaxPoints);

/// <summary>The AI's judgment for one criterion, exactly as the model returned it.</summary>
public sealed record AiCriterionResult(
    string CriterionId,
    CriterionOutcome Outcome,
    int? Score,
    string Explanation,
    IReadOnlyList<int> EvidenceSegments);

/// <summary>The full structured response the model must return (enforced by JSON Schema).</summary>
public sealed record AiEvaluation(int? AgentSpeaker, IReadOnlyList<AiCriterionResult> Results);

public sealed record GroqEvaluationResult(
    string Model,
    string PromptVersion,
    AiEvaluation Evaluation,
    int? PromptTokens,
    int? CompletionTokens);
