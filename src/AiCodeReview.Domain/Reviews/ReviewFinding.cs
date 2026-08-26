using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Reviews;

/// <summary>
/// A single problem reported against a pull request.
///
/// Every finding stored here has already been validated against the real diff:
/// the file exists in the pull request and the line range falls inside a changed
/// hunk. Unvalidated model output never reaches this type.
/// </summary>
public sealed class ReviewFinding : Entity
{
    private ReviewFinding()
    {
    }

    public Guid ReviewRunId { get; private set; }

    /// <summary>Denormalised for analytics; see <see cref="ReviewRun.CodeRepositoryId"/>.</summary>
    public Guid CodeRepositoryId { get; private set; }

    public FindingSource Source { get; private set; }

    public FindingCategory Category { get; private set; }

    public FindingSeverity Severity { get; private set; }

    public FindingConfidence Confidence { get; private set; }

    /// <summary>Set when a deterministic rule produced the finding, for example <c>SEC001</c>.</summary>
    public string? RuleCode { get; private set; }

    public string Title { get; private set; } = null!;

    public string FilePath { get; private set; } = null!;

    public int StartLine { get; private set; }

    public int EndLine { get; private set; }

    public string Description { get; private set; } = null!;

    public string? Reasoning { get; private set; }

    public string? Recommendation { get; private set; }

    public string? SuggestedFix { get; private set; }

    /// <summary>
    /// Stable hash over repository, path, rule and normalised title. Lets the
    /// same problem be recognised across runs, so a dismissal survives a rerun
    /// and duplicates between sources can be merged.
    /// </summary>
    public string Fingerprint { get; private set; } = null!;

    public FindingStatus Status { get; private set; }

    public DateTimeOffset? StatusChangedAtUtc { get; private set; }

    public Guid? StatusChangedByUserId { get; private set; }

    public bool IsSuppressed { get; private set; }

    public string? SuppressionReason { get; private set; }

    public static ReviewFinding Create(
        Guid reviewRunId,
        Guid codeRepositoryId,
        FindingSource source,
        FindingCategory category,
        FindingSeverity severity,
        FindingConfidence confidence,
        string? ruleCode,
        string title,
        string filePath,
        int startLine,
        int endLine,
        string description,
        string? reasoning,
        string? recommendation,
        string? suggestedFix,
        string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startLine);

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endLine),
                $"End line {endLine} precedes start line {startLine}.");
        }

        return new ReviewFinding
        {
            Id = Guid.CreateVersion7(),
            ReviewRunId = reviewRunId,
            CodeRepositoryId = codeRepositoryId,
            Source = source,
            Category = category,
            Severity = severity,
            Confidence = confidence,
            RuleCode = ruleCode,
            Title = title,
            FilePath = filePath,
            StartLine = startLine,
            EndLine = endLine,
            Description = description,
            Reasoning = reasoning,
            Recommendation = recommendation,
            SuggestedFix = suggestedFix,
            Fingerprint = fingerprint,
            Status = FindingStatus.New
        };
    }

    public void ChangeStatus(FindingStatus status, Guid changedByUserId, DateTimeOffset atUtc)
    {
        if (status == FindingStatus.New)
        {
            throw new ArgumentException("A finding cannot be moved back to New.", nameof(status));
        }

        Status = status;
        StatusChangedByUserId = changedByUserId;
        StatusChangedAtUtc = atUtc;
    }

    /// <summary>
    /// Hides a finding the user has previously dismissed on this repository.
    /// Suppressed findings are retained rather than deleted so the suppression
    /// itself stays auditable and reversible.
    /// </summary>
    public void Suppress(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsSuppressed = true;
        SuppressionReason = reason;
    }
}
