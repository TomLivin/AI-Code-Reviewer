using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Jobs;

/// <summary>
/// A unit of durable background work (ADR-002).
///
/// Deliberately generic rather than review-specific: repository synchronisation
/// and comment publishing use the same table. The atomic claim itself is done in
/// SQL with <c>FOR UPDATE SKIP LOCKED</c>; this type models the state machine
/// that claim drives, so the transitions stay unit-testable.
///
/// Claiming takes a lease instead of deleting the row, so a worker that dies
/// mid-job has its work reclaimed once the lease expires rather than lost.
/// </summary>
public sealed class BackgroundJob : Entity
{
    public const int DefaultMaxAttempts = 5;

    private BackgroundJob()
    {
    }

    public JobType Type { get; private set; }

    public string PayloadJson { get; private set; } = null!;

    public JobState State { get; private set; }

    /// <summary>Earliest time this job may be claimed; how retry backoff is expressed.</summary>
    public DateTimeOffset AvailableAtUtc { get; private set; }

    public string? LockedBy { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public int Attempts { get; private set; }

    public int MaxAttempts { get; private set; }

    public string? LastError { get; private set; }

    public Guid CorrelationId { get; private set; }

    public static BackgroundJob Create(
        JobType type,
        string payloadJson,
        Guid correlationId,
        DateTimeOffset availableAtUtc,
        int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        return new BackgroundJob
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            PayloadJson = payloadJson,
            CorrelationId = correlationId,
            State = JobState.Pending,
            AvailableAtUtc = availableAtUtc,
            MaxAttempts = maxAttempts
        };
    }

    public void Claim(string workerId, DateTimeOffset leaseUntilUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        if (State is not JobState.Pending)
        {
            throw new InvalidOperationException($"A {State} job cannot be claimed.");
        }

        State = JobState.Running;
        LockedBy = workerId;
        LockedUntilUtc = leaseUntilUtc;
        Attempts++;
    }

    /// <summary>Heartbeat. A long job must keep extending its lease or be reclaimed.</summary>
    public void ExtendLease(DateTimeOffset leaseUntilUtc)
    {
        if (State is not JobState.Running)
        {
            throw new InvalidOperationException($"A {State} job has no lease to extend.");
        }

        LockedUntilUtc = leaseUntilUtc;
    }

    public void Succeed()
    {
        if (State is not JobState.Running)
        {
            throw new InvalidOperationException($"A {State} job cannot succeed.");
        }

        State = JobState.Succeeded;
        LockedBy = null;
        LockedUntilUtc = null;
    }

    /// <summary>
    /// Records a failure and either schedules a retry or dead-letters the job
    /// once the attempt budget is spent. Callers supply the backoff so the delay
    /// policy stays configurable rather than baked into the entity.
    /// </summary>
    public void Fail(string error, DateTimeOffset retryAtUtc)
    {
        if (State is not JobState.Running)
        {
            throw new InvalidOperationException($"A {State} job cannot fail.");
        }

        LastError = error;
        LockedBy = null;
        LockedUntilUtc = null;

        if (Attempts >= MaxAttempts)
        {
            State = JobState.DeadLettered;
            return;
        }

        State = JobState.Pending;
        AvailableAtUtc = retryAtUtc;
    }

    public bool IsLeaseExpiredAt(DateTimeOffset atUtc) =>
        State is JobState.Running && LockedUntilUtc is not null && LockedUntilUtc <= atUtc;

    /// <summary>Returns an abandoned job to the queue after its lease expired.</summary>
    public void ReleaseExpiredLease(DateTimeOffset atUtc)
    {
        if (!IsLeaseExpiredAt(atUtc))
        {
            throw new InvalidOperationException("The lease has not expired.");
        }

        LastError = "Lease expired; the worker holding this job stopped reporting.";
        LockedBy = null;
        LockedUntilUtc = null;
        State = Attempts >= MaxAttempts ? JobState.DeadLettered : JobState.Pending;
        AvailableAtUtc = atUtc;
    }
}
