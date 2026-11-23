namespace VeraCue.Api.Entities;

/// <summary>
/// One AI evaluation of a call against its template (table: Evaluations). One per call (MVP).
/// Totals are NOT stored: they're calculated from the criterion results, so there's
/// no second copy of a number that could get out of sync.
/// </summary>
public class Evaluation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CallId { get; set; }
    public Call Call { get; set; } = null!;

    /// <summary>Template name at evaluation time; editing the template later doesn't change history.</summary>
    public required string TemplateNameSnapshot { get; set; }

    /// <summary>e.g. "openai/gpt-oss-120b" — which model produced the AI results.</summary>
    public required string ModelName { get; set; }

    /// <summary>e.g. "v1" — ties results to the prompt that produced them.</summary>
    public required string PromptVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Set by "Mark as reviewed" (Phase 5), together with ReviewedByUserId.</summary>
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }

    public List<CriterionResult> Results { get; set; } = [];
}
