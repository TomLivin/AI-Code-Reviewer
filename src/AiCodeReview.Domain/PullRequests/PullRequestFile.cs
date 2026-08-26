using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.PullRequests;

/// <summary>
/// One changed file at one commit. The unified diff hunk is kept so analysis and
/// finding validation both work from the exact text the review was based on,
/// even after the branch moves on.
/// </summary>
public sealed class PullRequestFile : Entity
{
    private PullRequestFile()
    {
    }

    public Guid PullRequestId { get; private set; }

    public string HeadSha { get; private set; } = null!;

    public string Path { get; private set; } = null!;

    public string? PreviousPath { get; private set; }

    public FileChangeStatus ChangeStatus { get; private set; }

    public int Additions { get; private set; }

    public int Deletions { get; private set; }

    public string? BlobSha { get; private set; }

    public bool IsBinary { get; private set; }

    /// <summary>GitHub omits the patch for very large files; analysis must handle that.</summary>
    public bool IsTruncated { get; private set; }

    public string? Patch { get; private set; }

    public static PullRequestFile Create(
        Guid pullRequestId,
        string headSha,
        string path,
        string? previousPath,
        FileChangeStatus changeStatus,
        int additions,
        int deletions,
        string? blobSha,
        bool isBinary,
        bool isTruncated,
        string? patch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return new PullRequestFile
        {
            Id = Guid.CreateVersion7(),
            PullRequestId = pullRequestId,
            HeadSha = headSha,
            Path = path,
            PreviousPath = previousPath,
            ChangeStatus = changeStatus,
            Additions = additions,
            Deletions = deletions,
            BlobSha = blobSha,
            IsBinary = isBinary,
            IsTruncated = isTruncated,
            Patch = patch
        };
    }

    public bool IsAnalysable => !IsBinary && !IsTruncated && Patch is not null;
}
