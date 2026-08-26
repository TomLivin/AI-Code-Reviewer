using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Identity;

/// <summary>
/// A user's GitHub connection. Tokens are stored already encrypted; this type
/// never sees or returns plaintext, and no API response model exposes it.
/// </summary>
public sealed class GitHubAccount : Entity
{
    private GitHubAccount()
    {
    }

    public Guid UserId { get; private set; }

    public byte[] AccessTokenProtected { get; private set; } = null!;

    public byte[]? RefreshTokenProtected { get; private set; }

    public DateTimeOffset? TokenExpiresAtUtc { get; private set; }

    public string[] Scopes { get; private set; } = [];

    public DateTimeOffset ConnectedAtUtc { get; private set; }

    public static GitHubAccount Create(
        Guid userId,
        byte[] accessTokenProtected,
        byte[]? refreshTokenProtected,
        DateTimeOffset? tokenExpiresAtUtc,
        string[] scopes,
        DateTimeOffset connectedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(accessTokenProtected);
        ArgumentNullException.ThrowIfNull(scopes);

        if (accessTokenProtected.Length == 0)
        {
            throw new ArgumentException("A protected access token is required.", nameof(accessTokenProtected));
        }

        return new GitHubAccount
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            AccessTokenProtected = accessTokenProtected,
            RefreshTokenProtected = refreshTokenProtected,
            TokenExpiresAtUtc = tokenExpiresAtUtc,
            Scopes = scopes,
            ConnectedAtUtc = connectedAtUtc
        };
    }

    public void ReplaceTokens(
        byte[] accessTokenProtected,
        byte[]? refreshTokenProtected,
        DateTimeOffset? tokenExpiresAtUtc,
        string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(accessTokenProtected);
        ArgumentNullException.ThrowIfNull(scopes);

        AccessTokenProtected = accessTokenProtected;
        RefreshTokenProtected = refreshTokenProtected;
        TokenExpiresAtUtc = tokenExpiresAtUtc;
        Scopes = scopes;
    }

    public bool IsExpiredAt(DateTimeOffset atUtc) =>
        TokenExpiresAtUtc is not null && TokenExpiresAtUtc <= atUtc;
}
