namespace VeraCue.Api.DTOs.Templates;

// DTOs = the JSON shapes the API sends and receives. They are separate from the
// database entities, so the database can change without breaking the frontend.

// ---------- Responses ----------

/// <summary>One card on the QA Templates page.</summary>
public sealed record TemplateSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int CriteriaCount,
    int TotalPoints,
    IReadOnlyList<CriterionPreviewDto> Criteria,
    DateTime UpdatedAt);

/// <summary>Criterion chip on a template card, e.g. "Greeting · 10".</summary>
public sealed record CriterionPreviewDto(string Name, int MaxPoints);

/// <summary>A full template, as loaded into the Template Builder.</summary>
public sealed record TemplateDetailDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<CriterionDto> Criteria,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CriterionDto(
    Guid Id,
    string Name,
    string Instructions,
    int MaxPoints,
    int SortOrder);

// ---------- Requests ----------
// Text fields are nullable on purpose: missing values reach TemplateValidator,
// which returns friendly, field-specific messages instead of a generic error.

/// <summary>Body of POST /api/templates (create) and PUT /api/templates/{id} (save edits).</summary>
public sealed record SaveTemplateRequest(
    string? Name,
    string? Description,
    List<SaveCriterionRequest>? Criteria);

/// <summary>
/// One criterion in the order it should be saved (list position = SortOrder).
/// Id is set for an existing criterion and null for a new one.
/// </summary>
public sealed record SaveCriterionRequest(
    Guid? Id,
    string? Name,
    string? Instructions,
    int MaxPoints);
