using VeraCue.Api.Entities;
using VeraCue.Api.Services.Scoring;
using static VeraCue.Api.Entities.CriterionOutcome;

namespace VeraCue.Api.Tests;

/// <summary>
/// Score rules from the UI Page Specification and Database Design.
/// The first tests reproduce the worked example for call #024 exactly.
/// </summary>
public class ScoreCalculatorTests
{
    // Call #024 from the mockups:
    //   Greeting & introduction        10  AI Met 10
    //   Needs discovery                20  AI Partial 16
    //   Product explanation            20  AI Met 18
    //   Cancellation policy explained  20  AI Met 18
    //   Objection handling             15  AI Partial 9  -> manager override: Met 13
    //   Clear next steps & close       15  AI NeedsReview (no score)
    private static List<ScoreInput> Call024() =>
    [
        new(10, Met, 10),
        new(20, Partial, 16),
        new(20, Met, 18),
        new(20, Met, 18),
        new(15, Partial, 9, FinalOutcome: Met, FinalScore: 13),
        new(15, NeedsReview, null),
    ];

    [Fact]
    public void Worked_example_original_AI_score_is_71_of_85_84_percent()
    {
        var score = ScoreCalculator.Calculate(Call024());

        Assert.Equal(new ScoreTotal(71, 85, 84), score.Ai);
    }

    [Fact]
    public void Worked_example_current_score_is_75_of_85_88_percent_with_15_points_pending()
    {
        var score = ScoreCalculator.Calculate(Call024());

        Assert.Equal(new ScoreTotal(75, 85, 88), score.Current);
        Assert.Equal(15, score.PendingPoints);
        Assert.Equal(1, score.PendingCount);
        Assert.Equal(1, score.OverrideCount);
        Assert.False(score.CanMarkReviewed);
    }

    [Fact]
    public void Worked_example_raising_cancellation_policy_to_20_gives_77_of_85_91_percent()
    {
        var results = Call024();
        results[3] = results[3] with { FinalOutcome = Met, FinalScore = 20 };

        var score = ScoreCalculator.Calculate(results);

        Assert.Equal(new ScoreTotal(77, 85, 91), score.Current);
        Assert.Equal(new ScoreTotal(71, 85, 84), score.Ai); // the AI score never changes
    }

    [Fact]
    public void Worked_example_counts_4_met_1_partial_1_needs_review()
    {
        var score = ScoreCalculator.Calculate(Call024());

        Assert.Equal((4, 1, 0, 0), (score.MetCount, score.PartialCount, score.NotMetCount, score.NotApplicableCount));
    }

    [Fact]
    public void Not_applicable_is_left_out_of_both_sums()
    {
        var score = ScoreCalculator.Calculate([new(10, Met, 10), new(20, NotApplicable, null)]);

        Assert.Equal(new ScoreTotal(10, 10, 100), score.Current);
        Assert.Equal(0, score.PendingPoints);
    }

    [Fact]
    public void Needs_review_is_never_counted_as_a_pass()
    {
        var score = ScoreCalculator.Calculate([new(10, Met, 5), new(90, NeedsReview, null)]);

        // 5/10 = 50%, NOT (5+90)/100 = 95% and NOT 5/100 = 5%.
        Assert.Equal(new ScoreTotal(5, 10, 50), score.Current);
        Assert.Equal(90, score.PendingPoints);
    }

    [Fact]
    public void Not_met_counts_zero_points_out_of_its_maximum()
    {
        var score = ScoreCalculator.Calculate([new(10, Met, 10), new(10, NotMet, 0)]);

        Assert.Equal(new ScoreTotal(10, 20, 50), score.Current);
    }

    [Fact]
    public void Deciding_a_pending_criterion_moves_it_into_the_score_and_allows_review()
    {
        var results = Call024();
        results[5] = results[5] with { FinalOutcome = NotMet, FinalScore = 0 };

        var score = ScoreCalculator.Calculate(results);

        Assert.Equal(new ScoreTotal(75, 100, 75), score.Current);
        Assert.Equal(0, score.PendingPoints);
        Assert.True(score.CanMarkReviewed);
        Assert.Equal(new ScoreTotal(71, 85, 84), score.Ai);
    }

    [Fact]
    public void Overriding_to_not_applicable_removes_the_criterion_from_the_total()
    {
        var results = Call024();
        results[5] = results[5] with { FinalOutcome = NotApplicable, FinalScore = null };

        var score = ScoreCalculator.Calculate(results);

        Assert.Equal(new ScoreTotal(75, 85, 88), score.Current);
        Assert.True(score.CanMarkReviewed);
    }

    [Fact]
    public void Nothing_scored_means_no_percentage_rather_than_0_or_100()
    {
        var score = ScoreCalculator.Calculate([new(10, NotApplicable, null), new(10, NeedsReview, null)]);

        Assert.Null(score.Current.Percent);
        Assert.Null(score.Ai.Percent);
        Assert.Null(ScoreCalculator.Calculate([]).Current.Percent);
    }

    [Fact]
    public void Percentages_round_half_up()
    {
        // 1/8 = 12.5% -> 13%, 7/8 = 87.5% -> 88%
        Assert.Equal(13, ScoreCalculator.Calculate([new(8, Partial, 1)]).Current.Percent);
        Assert.Equal(88, ScoreCalculator.Calculate([new(8, Partial, 7)]).Current.Percent);
    }
}
