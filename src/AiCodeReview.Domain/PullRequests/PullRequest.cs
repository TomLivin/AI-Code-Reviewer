using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.PullRequests;

/// <summary>
/// A pull request mirrored from GitHub. Identified naturally by
/// (repository, number), which is what makes synchronisation an idempotent
/// upsert rather than a duplicate-detection problem.
/// </summary>
public sealed class PullRequest : Entity
{
    private readonly List<PullRequestFile> _files = [];

    private PullRequest()
    {
    }

    public Guid CodeRepositoryId { get; private set; }

    public long GitHubPullRequestId { get; private set; }

    public int Number { get; private set; }

    public string Title { get; private set; } = null!;

    public string AuthorLogin { get; private set; } = null!;

    public PullRequestState State { get; private set; }

    public bool IsDraft { get; private set; }

    /// <summary>Commit the pull request currently points at. A review is always tied to one.</summary>
    public string HeadSha { get; private set; } = null!;

    public string BaseSha { get; private set; } = null!;

    public string HeadRef { get; private set; } = null!;

    public string BaseRef { get; private set; } = null!;

    public int Additions { get; private set; }

    public int Deletions { get; private set; }

    public int ChangedFiles { get; private set; }

    public DateTimeOffset GitHubUpdatedAtUtc { get; private set; }

    public DateTimeOffset LastSyncedAtUtc { get; private set; }

    public IReadOnlyCollection<PullRequestFile> Files => _files.AsReadOnly();

    public static PullRequest Create(
        Guid codeRepositoryId,
        long gitHubPullRequestId,
        int number,
        string title,
        string authorLogin,
        PullRequestState state,
        bool isDraft,
        string headSha,
        string baseSha,
        string headRef,
        string baseRef,
        int additions,
        int deletions,
        int changedFiles,
        DateTimeOffset gitHubUpdatedAtUtc,
        DateTimeOffset syncedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseSha);

        return new PullRequest
        {
            Id = Guid.CreateVersion7(),
            CodeRepositoryId = codeRepositoryId,
            GitHubPullRequestId = gitHubPullRequestId,
            Number = number,
            Title = title,
            AuthorLogin = authorLogin,
            State = state,
            IsDraft = isDraft,
            HeadSha = headSha,
            BaseSha = baseSha,
            HeadRef = headRef,
            BaseRef = baseRef,
            Additions = additions,
            Deletions = deletions,
            ChangedFiles = changedFiles,
            GitHubUpdatedAtUtc = gitHubUpdatedAtUtc,
            LastSyncedAtUtc = syncedAtUtc
        };
    }

    public void SyncFrom(
        string title,
        PullRequestState state,
        bool isDraft,
        string headSha,
        string baseSha,
        int additions,
        int deletions,
        int changedFiles,
        DateTimeOffset gitHubUpdatedAtUtc,
        DateTimeOffset syncedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);

        Title = title;
        State = state;
        IsDraft = isDraft;
        HeadSha = headSha;
        BaseSha = baseSha;
        Additions = additions;
        Deletions = deletions;
        ChangedFiles = changedFiles;
        GitHubUpdatedAtUtc = gitHubUpdatedAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
    }

    /// <summary>
    /// File snapshots are keyed by commit, so pushing new commits adds a new
    /// snapshot instead of destroying the one an earlier review was based on.
    /// </summary>
    public void ReplaceFileSnapshot(string headSha, IEnumerable<PullRequestFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);
        ArgumentNullException.ThrowIfNull(files);

        _files.RemoveAll(file => string.Equals(file.HeadSha, headSha, StringComparison.Ordinal));
        _files.AddRange(files);
    }
}
