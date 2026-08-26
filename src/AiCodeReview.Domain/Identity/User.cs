using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Identity;

/// <summary>
/// A person using the system. There is no password: GitHub is the only identity
/// provider (ADR-006), so a user is created on first successful OAuth callback.
/// </summary>
public sealed class User : Entity
{
    private User()
    {
    }

    public long GitHubUserId { get; private set; }

    public string Login { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? AvatarUrl { get; private set; }

    public DateTimeOffset? LastSignedInAtUtc { get; private set; }

    public static User Create(long gitHubUserId, string login, string? email, string? avatarUrl)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gitHubUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);

        return new User
        {
            Id = Guid.CreateVersion7(),
            GitHubUserId = gitHubUserId,
            Login = login,
            Email = email,
            AvatarUrl = avatarUrl
        };
    }

    /// <summary>
    /// GitHub is the source of truth for profile data, so a sign-in refreshes it
    /// rather than trusting whatever was stored at first connect.
    /// </summary>
    public void RecordSignIn(string login, string? email, string? avatarUrl, DateTimeOffset atUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);

        Login = login;
        Email = email;
        AvatarUrl = avatarUrl;
        LastSignedInAtUtc = atUtc;
    }
}
