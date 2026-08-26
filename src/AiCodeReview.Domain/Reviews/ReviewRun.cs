using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Reviews;

/// <summary>
/// One execution of the review pipeline against one commit of one pull request.
///
/// A run *is* the review (ADR-009): there is no separate parent entity, because
/// one that always owns exactly one run and holds no state of its own would be
/// a join table. Re-reviewing after a push creates a new run, so history is the
/// ordered set of runs for a pull request.
/// </summary>
public sealed class ReviewRun : Entity
{
    private readonly List<ReviewFinding> _findings = [];

    private ReviewRun()
    {
    }

    public Guid PullRequestId { get; private set; }

    /// <summary>
    /// Denormalised from the pull request. Analytics filters findings and runs by
    /// repository, and carrying the key here removes a two-table join from the
    /// hottest read path.
    /// </summary>
    public Guid CodeRepositoryId { get; private set; }

    public string HeadSha { get; private set; } = null!;

    public ReviewRunStatus Status { get; private set; }

    public ReviewTrigger Trigger { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public Guid CorrelationId { get; private set; }

    public int? RiskScore { get; private set; }

    public RiskBand? RiskBand { get; private set; }

    /// <summary>Serialised, itemised explanation of how the score was reached.</summary>
    public string? ScoreBreakdownJson { get; private set; }

    public string? Summary { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset QueuedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public int? DurationMs { get; private set; }

    public IReadOnlyCollection<ReviewFinding> Findings => _findings.AsReadOnly();

    /// <summary>
    /// Backs the partial unique index that prevents two concurrent runs for the
    /// same pull request and commit.
    /// </summary>
    public bool IsActive => Status is ReviewRunStatus.Queued or ReviewRunStatus.Running;

    public static ReviewRun Queue(
        Guid pullRequestId,
        Guid codeRepositoryId,
        string headSha,
        ReviewTrigger trigger,
        Guid requestedByUserId,
        Guid correlationId,
        DateTimeOffset queuedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);

        return new ReviewRun
        {
            Id = Guid.CreateVersion7(),
            PullRequestId = pullRequestId,
            CodeRepositoryId = codeRepositoryId,
            HeadSha = headSha,
            Status = ReviewRunStatus.Queued,
            Trigger = trigger,
            RequestedByUserId = requestedByUserId,
            CorrelationId = correlationId,
            QueuedAtUtc = queuedAtUtc
        };
    }

    public void MarkRunning(DateTimeOffset atUtc)
    {
        EnsureTransitionFrom(ReviewRunStatus.Queued, ReviewRunStatus.Running);

        Status = ReviewRunStatus.Running;
        StartedAtUtc = atUtc;
    }

    public void MarkSucceeded(
        int riskScore,
        RiskBand riskBand,
        string scoreBreakdownJson,
        string summary,
        DateTimeOffset atUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(riskScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(riskScore, 100);
        ArgumentException.ThrowIfNullOrWhiteSpace(scoreBreakdownJson);

        EnsureTransitionFrom(ReviewRunStatus.Running, ReviewRunStatus.Succeeded);

        Status = ReviewRunStatus.Succeeded;
        RiskScore = riskScore;
        RiskBand = riskBand;
        ScoreBreakdownJson = scoreBreakdownJson;
        Summary = summary;
        Complete(atUtc);
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset atUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if (!IsActive)
        {
            throw new InvalidOperationException($"A {Status} run cannot fail.");
        }

        Status = ReviewRunStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Complete(atUtc);
    }

    public void MarkCancelled(DateTimeOffset atUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"A {Status} run cannot be cancelled.");
        }

        Status = ReviewRunStatus.Cancelled;
        Complete(atUtc);
    }

    public void AddFindings(IEnumerable<ReviewFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        if (Status is not ReviewRunStatus.Running)
        {
            throw new InvalidOperationException("Findings may only be added while the run is in progress.");
        }

        _findings.AddRange(findings);
    }

    private void Complete(DateTimeOffset atUtc)
    {
        CompletedAtUtc = atUtc;
        DurationMs = StartedAtUtc is null
            ? null
            : (int)Math.Max(0, (atUtc - StartedAtUtc.Value).TotalMilliseconds);
    }

    private void EnsureTransitionFrom(ReviewRunStatus expected, ReviewRunStatus target)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot move a review run from {Status} to {target}; expected {expected}.");
        }
    }
}
