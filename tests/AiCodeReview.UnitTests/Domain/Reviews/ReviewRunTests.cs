using AiCodeReview.Domain.Reviews;

namespace AiCodeReview.UnitTests.Domain.Reviews;

public sealed class ReviewRunTests
{
    private static readonly DateTimeOffset QueuedAt = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);

    private static ReviewRun Queue() => ReviewRun.Queue(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        new string('a', 40),
        ReviewTrigger.Manual,
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        QueuedAt);

    [Fact]
    public void A_queued_run_is_active_and_carries_no_result()
    {
        ReviewRun run = Queue();

        run.Status.ShouldBe(ReviewRunStatus.Queued);
        run.IsActive.ShouldBeTrue();
        run.RiskScore.ShouldBeNull();
        run.StartedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void A_successful_run_records_its_score_and_duration()
    {
        ReviewRun run = Queue();

        run.MarkRunning(QueuedAt.AddSeconds(2));
        run.MarkSucceeded(71, RiskBand.High, """{"base":113.4}""", "Auth check missing.", QueuedAt.AddSeconds(47));

        run.Status.ShouldBe(ReviewRunStatus.Succeeded);
        run.IsActive.ShouldBeFalse();
        run.RiskScore.ShouldBe(71);
        run.RiskBand.ShouldBe(RiskBand.High);
        run.DurationMs.ShouldBe(45_000);
    }

    [Fact]
    public void A_run_cannot_succeed_without_having_started()
    {
        // Guards against a worker writing results for a job it never claimed,
        // which would otherwise silently produce a run with no duration.
        ReviewRun run = Queue();

        Should.Throw<InvalidOperationException>(
            () => run.MarkSucceeded(50, RiskBand.Moderate, "{}", "…", QueuedAt));
    }

    [Fact]
    public void A_completed_run_cannot_be_failed_or_cancelled_afterwards()
    {
        ReviewRun run = Queue();
        run.MarkRunning(QueuedAt);
        run.MarkSucceeded(10, RiskBand.Low, "{}", "Clean.", QueuedAt.AddSeconds(5));

        Should.Throw<InvalidOperationException>(() => run.MarkFailed("boom", "…", QueuedAt.AddSeconds(6)));
        Should.Throw<InvalidOperationException>(() => run.MarkCancelled(QueuedAt.AddSeconds(6)));
    }

    [Fact]
    public void A_run_that_fails_before_starting_records_no_duration()
    {
        ReviewRun run = Queue();

        run.MarkFailed("github.unreachable", "GitHub could not be reached.", QueuedAt.AddSeconds(3));

        run.Status.ShouldBe(ReviewRunStatus.Failed);
        run.ErrorCode.ShouldBe("github.unreachable");
        run.DurationMs.ShouldBeNull();
    }

    [Fact]
    public void A_risk_score_outside_zero_to_one_hundred_is_rejected()
    {
        ReviewRun run = Queue();
        run.MarkRunning(QueuedAt);

        Should.Throw<ArgumentOutOfRangeException>(
            () => run.MarkSucceeded(101, RiskBand.Critical, "{}", "…", QueuedAt.AddSeconds(1)));
    }

    [Fact]
    public void Findings_may_only_be_added_while_the_run_is_in_progress()
    {
        ReviewRun run = Queue();

        Should.Throw<InvalidOperationException>(() => run.AddFindings([]));
    }
}
