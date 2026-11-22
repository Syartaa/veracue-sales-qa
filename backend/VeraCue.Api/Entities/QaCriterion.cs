namespace VeraCue.Api.Entities;

/// <summary>One criterion in a template (table: QaCriteria).</summary>
public class QaCriterion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TemplateId { get; set; }

    public QaTemplate Template { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>What the AI must judge, written as a clear instruction.</summary>
    public required string Instructions { get; set; }

    /// <summary>1–100. Acts as the criterion's weight in the overall score.</summary>
    public int MaxPoints { get; set; }

    /// <summary>Display order and the order criteria are sent to the AI.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
