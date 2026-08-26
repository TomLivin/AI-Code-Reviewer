using AiCodeReview.Domain.Jobs;

namespace AiCodeReview.UnitTests.Domain.Jobs;

public sealed class BackgroundJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);

    private static BackgroundJob Create(int maxAttempts = 3) =>
        BackgroundJob.Create(JobType.ReviewRun, """{"reviewRunId":"x"}""", Guid.CreateVersion7(), Now, maxAttempts);

    [Fact]
    public void A_new_job_is_pending_and_immediately_claimable()
    {
        BackgroundJob job = Create();

        job.State.ShouldBe(JobState.Pending);
        job.Attempts.ShouldBe(0);
        job.AvailableAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Claiming_takes_a_lease_and_counts_the_attempt()
    {
        BackgroundJob job = Create();

        job.Claim("worker-1", Now.AddMinutes(5));

        job.State.ShouldBe(JobState.Running);
        job.LockedBy.ShouldBe("worker-1");
        job.Attempts.ShouldBe(1);
    }

    [Fact]
    public void A_running_job_cannot_be_claimed_twice()
    {
        BackgroundJob job = Create();
        job.Claim("worker-1", Now.AddMinutes(5));

        Should.Throw<InvalidOperationException>(() => job.Claim("worker-2", Now.AddMinutes(5)));
    }

    [Fact]
    public void Failing_below_the_attempt_budget_schedules_a_retry()
    {
        BackgroundJob job = Create(maxAttempts: 3);
        job.Claim("worker-1", Now.AddMinutes(5));

        job.Fail("Rate limited by GitHub.", Now.AddSeconds(30));

        job.State.ShouldBe(JobState.Pending);
        job.AvailableAtUtc.ShouldBe(Now.AddSeconds(30));
        job.LockedBy.ShouldBeNull("the lease must be released so another worker can pick it up");
    }

    [Fact]
    public void Failing_at_the_attempt_budget_dead_letters_the_job()
    {
        // Without this a permanently failing job retries for ever, and on a
        // review that means paying for model calls that can never succeed.
        BackgroundJob job = Create(maxAttempts: 2);

        job.Claim("worker-1", Now.AddMinutes(5));
        job.Fail("first", Now.AddSeconds(30));
        job.Claim("worker-1", Now.AddMinutes(5));
        job.Fail("second", Now.AddSeconds(60));

        job.State.ShouldBe(JobState.DeadLettered);
    }

    [Fact]
    public void An_expired_lease_returns_the_job_to_the_queue()
    {
        // Models a worker that was killed mid-job: the row must become
        // claimable again rather than sitting in Running for ever.
        BackgroundJob job = Create();
        job.Claim("worker-1", Now.AddMinutes(5));

        job.IsLeaseExpiredAt(Now.AddMinutes(4)).ShouldBeFalse();
        job.IsLeaseExpiredAt(Now.AddMinutes(6)).ShouldBeTrue();

        job.ReleaseExpiredLease(Now.AddMinutes(6));

        job.State.ShouldBe(JobState.Pending);
        job.LockedBy.ShouldBeNull();
        job.LastError.ShouldNotBeNull();
    }

    [Fact]
    public void Extending_a_lease_keeps_a_long_running_job_from_being_reclaimed()
    {
        BackgroundJob job = Create();
        job.Claim("worker-1", Now.AddMinutes(5));

        job.ExtendLease(Now.AddMinutes(10));

        job.IsLeaseExpiredAt(Now.AddMinutes(6)).ShouldBeFalse();
    }
}
