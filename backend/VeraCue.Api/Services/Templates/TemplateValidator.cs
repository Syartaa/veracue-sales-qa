using VeraCue.Api.DTOs.Templates;

namespace VeraCue.Api.Services.Templates;

/// <summary>
/// Validates a create/update request before anything touches the database.
/// Error keys match the request's JSON paths (e.g. "criteria[5].instructions"),
/// so the Template Builder can highlight exactly the right field.
/// The frontend repeats these rules for instant feedback; this is the one that counts.
/// </summary>
public static class TemplateValidator
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 1000;
    public const int InstructionsMaxLength = 2000;
    public const int MinPoints = 1;
    public const int MaxPoints = 100;
    public const int MaxCriteria = 30; // keeps the AI prompt focused and within token limits

    public static Dictionary<string, string[]> Validate(SaveTemplateRequest request)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list)) errors[field] = list = [];
            list.Add(message);
        }

        // Template
        if (string.IsNullOrWhiteSpace(request.Name))
            Add("name", "Template name is required.");
        else if (request.Name.Trim().Length > NameMaxLength)
            Add("name", $"Template name can be at most {NameMaxLength} characters.");

        if (request.Description?.Trim().Length > DescriptionMaxLength)
            Add("description", $"Description can be at most {DescriptionMaxLength} characters.");

        // Criteria list
        var criteria = request.Criteria ?? [];
        if (criteria.Count == 0)
            Add("criteria", "Add at least one criterion.");
        else if (criteria.Count > MaxCriteria)
            Add("criteria", $"A template can have at most {MaxCriteria} criteria.");

        // Each criterion
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<Guid>();

        for (var i = 0; i < criteria.Count; i++)
        {
            var criterion = criteria[i];
            var key = $"criteria[{i}]";

            if (string.IsNullOrWhiteSpace(criterion.Name))
                Add($"{key}.name", "Criterion name is required.");
            else if (criterion.Name.Trim().Length > NameMaxLength)
                Add($"{key}.name", $"Criterion name can be at most {NameMaxLength} characters.");
            else if (!seenNames.Add(criterion.Name.Trim()))
                Add($"{key}.name", "Another criterion in this template already has this name.");

            if (string.IsNullOrWhiteSpace(criterion.Instructions))
                Add($"{key}.instructions", "Instructions are required so the AI knows what to check.");
            else if (criterion.Instructions.Trim().Length > InstructionsMaxLength)
                Add($"{key}.instructions", $"Instructions can be at most {InstructionsMaxLength} characters.");

            if (criterion.MaxPoints is < MinPoints or > MaxPoints)
                Add($"{key}.maxPoints", $"Max points must be between {MinPoints} and {MaxPoints}.");

            if (criterion.Id is { } id && !seenIds.Add(id))
                Add($"{key}.id", "This criterion appears twice in the list.");
        }

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }
}
