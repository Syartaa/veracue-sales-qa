using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Services.Calls;
using static VeraCue.Api.Entities.CriterionOutcome;

namespace VeraCue.Api.Tests;

/// <summary>Rules for a manager's override (see OverrideValidator).</summary>
public class OverrideValidatorTests
{
    private const int Max = 20;
    private const string Reason = "Our standard only requires the cooling-off period to be stated proactively.";

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Met_with_a_score_in_range_and_a_reason_is_valid(int score)
    {
        Assert.Empty(OverrideValidator.Validate(new OverrideRequest(Met, score, Reason, null), Max));
    }

    [Fact]
    public void Not_applicable_without_a_score_is_valid()
    {
        Assert.Empty(OverrideValidator.Validate(new OverrideRequest(NotApplicable, null, Reason, null), Max));
    }

    [Fact]
    public void Not_applicable_with_a_score_is_rejected()
    {
        Assert.True(OverrideValidator.Validate(new OverrideRequest(NotApplicable, 5, Reason, null), Max).ContainsKey("score"));
    }

    [Fact]
    public void Outcome_is_required_and_cannot_be_needs_review()
    {
        Assert.True(OverrideValidator.Validate(new OverrideRequest(null, 5, Reason, null), Max).ContainsKey("outcome"));
        Assert.True(OverrideValidator.Validate(new OverrideRequest(NeedsReview, null, Reason, null), Max).ContainsKey("outcome"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Scored_outcomes_need_a_score_from_0_to_max(int? score)
    {
        Assert.True(OverrideValidator.Validate(new OverrideRequest(Partial, score, Reason, null), Max).ContainsKey("score"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_reason_is_always_required(string? reason)
    {
        var errors = OverrideValidator.Validate(new OverrideRequest(Met, 20, reason, null), Max);

        Assert.Equal(["A reason is required for every override."], errors["reason"]);
    }

    [Fact]
    public void Overlong_reason_is_rejected()
    {
        var errors = OverrideValidator.Validate(new OverrideRequest(Met, 20, new string('a', 1001), null), Max);

        Assert.True(errors.ContainsKey("reason"));
    }
}
