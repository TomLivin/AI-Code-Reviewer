using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Repositories;

/// <summary>
/// A GitHub repository a user has connected. Named <c>CodeRepository</c> rather
/// than <c>Repository</c> so it is never confused with the repository pattern.
///
/// Disconnecting is a soft delete: review history and findings must survive it,
/// and reconnecting should restore rather than duplicate.
/// </summary>
public sealed class CodeRepository : Entity
{
    private CodeRepository()
    {
    }

    public Guid OwnerUserId { get; private set; }

    public long GitHubRepositoryId { get; private set; }

    public string FullName { get; private set; } = null!;

    public string DefaultBranch { get; private set; } = null!;

    public bool IsPrivate { get; private set; }

    public string? PrimaryLanguage { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public bool IsConnected => DeletedAtUtc is null;

    public static CodeRepository Connect(
        Guid ownerUserId,
        long gitHubRepositoryId,
        string fullName,
        string defaultBranch,
        bool isPrivate,
        string? primaryLanguage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gitHubRepositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);

        return new CodeRepository
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            GitHubRepositoryId = gitHubRepositoryId,
            FullName = fullName,
            DefaultBranch = defaultBranch,
            IsPrivate = isPrivate,
            PrimaryLanguage = primaryLanguage
        };
    }

    public void RefreshMetadata(string fullName, string defaultBranch, bool isPrivate, string? primaryLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);

        FullName = fullName;
        DefaultBranch = defaultBranch;
        IsPrivate = isPrivate;
        PrimaryLanguage = primaryLanguage;
    }

    public void Disconnect(DateTimeOffset atUtc) => DeletedAtUtc ??= atUtc;

    public void Reconnect() => DeletedAtUtc = null;
}
