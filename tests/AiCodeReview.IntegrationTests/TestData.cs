using AiCodeReview.Domain.Identity;
using Identity = AiCodeReview.Domain.Identity;
using Domain = AiCodeReview.Domain;
using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;

namespace AiCodeReview.IntegrationTests;

/// <summary>
/// Builds realistic graphs for tests. Identifiers are unique per call so tests
/// sharing one container cannot collide on the natural-key unique indexes.
/// </summary>
internal static class TestData
{
    private static int _sequence;

    internal static long NextExternalId() => 1_000_000 + Interlocked.Increment(ref _sequence);

    internal static User User() =>
        Identity.User.Create(NextExternalId(), $"octocat-{NextExternalId()}", "octocat@example.com", null);

    internal static CodeRepository Repository(Guid ownerUserId) =>
        CodeRepository.Connect(ownerUserId, NextExternalId(), $"octocat/repo-{NextExternalId()}", "main", false, "C#");

    internal static PullRequest PullRequest(Guid repositoryId, string headSha) =>
        Domain.PullRequests.PullRequest.Create(
            repositoryId,
            NextExternalId(),
            (int)NextExternalId(),
            "Add payment validation",
            "octocat",
            PullRequestState.Open,
            isDraft: false,
            headSha,
            baseSha: new string('b', 40),
            headRef: "feature/payment-validation",
            baseRef: "main",
            additions: 120,
            deletions: 14,
            changedFiles: 5,
            gitHubUpdatedAtUtc: DateTimeOffset.UtcNow,
            syncedAtUtc: DateTimeOffset.UtcNow);

    internal static ReviewRun QueuedRun(Guid pullRequestId, Guid repositoryId, Guid userId, string headSha) =>
        ReviewRun.Queue(
            pullRequestId,
            repositoryId,
            headSha,
            ReviewTrigger.Manual,
            userId,
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow);

    internal static string CommitSha(char fill = 'a') => new(fill, 40);
}
