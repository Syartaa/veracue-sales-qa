using VeraCue.Api.DTOs.Templates;
using VeraCue.Api.Services.Templates;

namespace VeraCue.Api.Tests;

/// <summary>Rules a template must meet before it is saved (see TemplateValidator).</summary>
public class TemplateValidatorTests
{
    private static SaveCriterionRequest Criterion(
        string? name = "Greeting & introduction",
        string? instructions = "Agent greets the customer and gives their name and the company name.",
        int maxPoints = 10,
        Guid? id = null) => new(id, name, instructions, maxPoints);

    private static SaveTemplateRequest Template(
        string? name = "Life Insurance Sales QA",
        string? description = null,
        params SaveCriterionRequest[] criteria) =>
        new(name, description, criteria.Length == 0 ? [Criterion()] : [.. criteria]);

    [Fact]
    public void Valid_template_has_no_errors()
    {
        var errors = TemplateValidator.Validate(Template(criteria:
        [
            Criterion("Greeting", maxPoints: 10),
            Criterion("Needs discovery", maxPoints: 20),
        ]));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string? name)
    {
        var errors = TemplateValidator.Validate(Template(name: name));

        Assert.Equal(["Template name is required."], errors["name"]);
    }

    [Fact]
    public void Name_longer_than_150_characters_is_rejected()
    {
        var errors = TemplateValidator.Validate(Template(name: new string('a', 151)));

        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void At_least_one_criterion_is_required()
    {
        var errors = TemplateValidator.Validate(new SaveTemplateRequest("Sales QA", null, []));

        Assert.Equal(["Add at least one criterion."], errors["criteria"]);
    }

    [Fact]
    public void Missing_criteria_list_is_treated_as_empty()
    {
        var errors = TemplateValidator.Validate(new SaveTemplateRequest("Sales QA", null, null));

        Assert.True(errors.ContainsKey("criteria"));
    }

    [Fact]
    public void More_than_30_criteria_is_rejected()
    {
        var criteria = Enumerable.Range(1, 31).Select(i => Criterion($"Criterion {i}")).ToArray();

        var errors = TemplateValidator.Validate(Template(criteria: criteria));

        Assert.True(errors.ContainsKey("criteria"));
    }

    [Fact]
    public void Empty_instructions_point_to_the_exact_criterion()
    {
        // Matches the Template Builder mockup: criterion 6 has no instructions.
        var errors = TemplateValidator.Validate(Template(criteria:
        [
            Criterion("Greeting"),
            Criterion("Clear next steps & close", instructions: "  "),
        ]));

        Assert.Equal(["Instructions are required so the AI knows what to check."], errors["criteria[1].instructions"]);
        Assert.Single(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void Max_points_outside_1_to_100_is_rejected(int points)
    {
        var errors = TemplateValidator.Validate(Template(criteria: Criterion(maxPoints: points)));

        Assert.Equal(["Max points must be between 1 and 100."], errors["criteria[0].maxPoints"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Max_points_at_the_limits_is_accepted(int points)
    {
        var errors = TemplateValidator.Validate(Template(criteria: Criterion(maxPoints: points)));

        Assert.Empty(errors);
    }

    [Fact]
    public void Duplicate_criterion_names_are_rejected_ignoring_case()
    {
        var errors = TemplateValidator.Validate(Template(criteria:
        [
            Criterion("Greeting"),
            Criterion("greeting "),
        ]));

        Assert.True(errors.ContainsKey("criteria[1].name"));
        Assert.False(errors.ContainsKey("criteria[0].name"));
    }

    [Fact]
    public void The_same_criterion_id_twice_is_rejected()
    {
        var id = Guid.NewGuid();

        var errors = TemplateValidator.Validate(Template(criteria:
        [
            Criterion("Greeting", id: id),
            Criterion("Needs discovery", id: id),
        ]));

        Assert.True(errors.ContainsKey("criteria[1].id"));
    }

    [Fact]
    public void Several_problems_are_all_reported_at_once()
    {
        var errors = TemplateValidator.Validate(new SaveTemplateRequest("", null,
        [
            Criterion(name: "", instructions: "", maxPoints: 0),
        ]));

        Assert.Equal(
            ["criteria[0].instructions", "criteria[0].maxPoints", "criteria[0].name", "name"],
            errors.Keys.Order());
    }
}
