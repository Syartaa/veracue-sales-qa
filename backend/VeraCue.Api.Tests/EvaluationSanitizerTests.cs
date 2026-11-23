using VeraCue.Api.Entities;
using VeraCue.Api.Services.Evaluation;
using static VeraCue.Api.Entities.CriterionOutcome;

namespace VeraCue.Api.Tests;

/// <summary>The backend never trusts the AI: these rules decide what is saved (see EvaluationSanitizer).</summary>
public class EvaluationSanitizerTests
{
    private static readonly CriterionInput[] Criteria =
    [
        new("c1", "Greeting", "Agent greets and introduces themselves.", 10),
        new("c2", "Cancellation policy", "Agent explains the cooling-off period.", 20),
    ];

    private const int SegmentCount = 30;
    private static readonly int[] Speakers = [0, 1];

    private static AiCriterionResult Answer(string id, CriterionOutcome outcome, int? score, params int[] evidence) =>
        new(id, outcome, score, "Explanation from the AI.", evidence);

    private static IReadOnlyList<SanitizedResult> Run(params AiCriterionResult[] answers) =>
        EvaluationSanitizer.Sanitize(new AiEvaluation(0, answers), Criteria, SegmentCount, Speakers).Results;

    [Fact]
    public void Valid_answers_are_kept_unchanged()
    {
        var results = Run(Answer("c1", Met, 10, 0, 1), Answer("c2", Partial, 12, 27));

        Assert.Equal((Met, (int?)10, false), (results[0].Outcome, results[0].Score, results[0].Downgraded));
        Assert.Equal([0, 1], results[0].EvidenceSegments);
        Assert.Equal((Partial, (int?)12), (results[1].Outcome, results[1].Score));
    }

    [Fact]
    public void Evidence_pointing_at_segments_that_do_not_exist_is_dropped()
    {
        var results = Run(Answer("c1", Met, 10, 0, 99, -1, 0), Answer("c2", NotMet, 0));

        Assert.Equal([0], results[0].EvidenceSegments); // 99 and -1 removed, duplicate 0 removed
        Assert.Equal(Met, results[0].Outcome);
    }

    [Fact]
    public void Met_without_any_valid_evidence_becomes_needs_review_with_no_score()
    {
        var results = Run(Answer("c1", Met, 10, 99), Answer("c2", NotMet, 0));

        Assert.Equal(NeedsReview, results[0].Outcome);
        Assert.Null(results[0].Score);
        Assert.True(results[0].Downgraded);
        Assert.StartsWith("Insufficient evidence", results[0].Explanation);
    }

    [Fact]
    public void Not_met_may_have_no_evidence()
    {
        var results = Run(Answer("c1", Met, 10, 0), Answer("c2", NotMet, 0));

        Assert.Equal((NotMet, (int?)0, false), (results[1].Outcome, results[1].Score, results[1].Downgraded));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Missing_or_out_of_range_score_becomes_needs_review(int? score)
    {
        var results = Run(Answer("c1", Met, 10, 0), Answer("c2", Met, score, 27));

        Assert.Equal(NeedsReview, results[1].Outcome);
        Assert.Null(results[1].Score);
        Assert.True(results[1].Downgraded);
    }

    [Fact]
    public void Not_applicable_and_needs_review_never_carry_a_score()
    {
        var results = Run(Answer("c1", NotApplicable, 5), Answer("c2", NeedsReview, 7));

        Assert.Null(results[0].Score);
        Assert.Null(results[1].Score);
        Assert.False(results[0].Downgraded);
    }

    [Fact]
    public void A_missing_criterion_becomes_needs_review_and_order_follows_the_template()
    {
        var results = Run(Answer("c2", Met, 18, 27)); // c1 missing; c2 answered

        Assert.Equal(2, results.Count);
        Assert.Equal((0, NeedsReview, true), (results[0].CriterionIndex, results[0].Outcome, results[0].Downgraded));
        Assert.Equal((1, Met), (results[1].CriterionIndex, results[1].Outcome));
    }

    [Fact]
    public void Unknown_and_duplicate_criterion_ids_are_ignored()
    {
        var results = Run(Answer("c1", Met, 10, 0), Answer("c1", NotMet, 0), Answer("c9", Met, 5, 1), Answer("c2", NotMet, 0));

        Assert.Equal(2, results.Count);
        Assert.Equal(Met, results[0].Outcome); // first answer for c1 wins
    }

    [Fact]
    public void Agent_speaker_is_kept_only_if_it_occurs_in_the_transcript()
    {
        var kept = EvaluationSanitizer.Sanitize(new AiEvaluation(1, []), Criteria, SegmentCount, Speakers).AgentSpeaker;
        var dropped = EvaluationSanitizer.Sanitize(new AiEvaluation(5, []), Criteria, SegmentCount, Speakers).AgentSpeaker;

        Assert.Equal((short)1, kept);
        Assert.Null(dropped);
    }
}
