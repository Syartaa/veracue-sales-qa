namespace VeraCue.Api.Entities;

/// <summary>Judgment for one criterion. Same list for the AI's outcome and the manager's.</summary>
public enum CriterionOutcome
{
    Met,
    Partial,
    NotMet,
    NotApplicable,
    NeedsReview,
}

/// <summary>
/// The result for one criterion in an evaluation (table: CriterionResults).
/// The AI columns are written once and NEVER changed; a manager override fills the
/// Final* / Override* columns instead, so the original AI judgment always stays on record.
/// </summary>
public class CriterionResult
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    /// <summary>Link back to the live criterion; becomes null if the criterion is later deleted.</summary>
    public Guid? CriterionId { get; set; }
    public QaCriterion? Criterion { get; set; }

    // ----- Snapshot of the criterion at evaluation time -----
    public int SortOrder { get; set; }
    public required string NameSnapshot { get; set; }
    public required string InstructionsSnapshot { get; set; }
    public int MaxPoints { get; set; }

    // ----- What the AI said (never modified after saving) -----
    public CriterionOutcome AiOutcome { get; set; }

    /// <summary>Null for NotApplicable and NeedsReview.</summary>
    public int? AiScore { get; set; }

    public required string AiExplanation { get; set; }

    // ----- Manager override (all null = not overridden) -----
    public CriterionOutcome? FinalOutcome { get; set; }

    /// <summary>Null when FinalOutcome is NotApplicable.</summary>
    public int? FinalScore { get; set; }

    public string? OverrideReason { get; set; }
    public Guid? OverriddenByUserId { get; set; }
    public User? OverriddenBy { get; set; }
    public DateTime? OverriddenAt { get; set; }

    /// <summary>Optimistic concurrency (PostgreSQL xmin): two tabs can't silently overwrite each other's override.</summary>
    public uint Version { get; set; }

    /// <summary>Transcript segments that support this judgment.</summary>
    public List<ResultEvidence> Evidence { get; set; } = [];
}
