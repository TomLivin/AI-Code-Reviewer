using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;
using AiCodeReview.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AiCodeReview.IntegrationTests;

/// <summary>
/// Verifies the invariants that live in the schema rather than in C#. Each of
/// these would pass silently against a fake provider.
/// </summary>
public sealed class SchemaInvariantTests(PostgresFixture fixture) : DatabaseTestBase(fixture)
{
    private const string DuplicateKeyState = "23505";

    [Fact]
    public async Task Migrations_create_every_table_the_model_declares()
    {
        await using AppDbContext context = NewContext();

        List<string> applied = (await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToList();
        applied.ShouldNotBeEmpty();

        string[] expected =
        [
            "users", "github_accounts", "code_repositories", "pull_requests",
            "pull_request_files", "review_runs", "review_findings", "ai_usages", "background_jobs"
        ];

        List<string> actual = await context.Database
            .SqlQuery<string>($"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync(TestContext.Current.CancellationToken);

        foreach (string table in expected)
        {
            actual.ShouldContain(table);
        }
    }

    [Fact]
    public async Task A_second_in_flight_run_for_the_same_commit_is_rejected_by_the_database()
    {
        // The scenario is a double-clicked Review button. Without the partial
        // unique index both inserts succeed and the pull request is analysed
        // twice, at twice the cost.
        await using AppDbContext context = NewContext();

        (Guid pullRequestId, Guid repositoryId, Guid userId, string headSha) = await SeedPullRequestAsync(context);

        context.ReviewRuns.Add(TestData.QueuedRun(pullRequestId, repositoryId, userId, headSha));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ReviewRuns.Add(TestData.QueuedRun(pullRequestId, repositoryId, userId, headSha));

        DbUpdateException exception = await Should.ThrowAsync<DbUpdateException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<PostgresException>()
            .SqlState.ShouldBe(DuplicateKeyState);
    }

    [Fact]
    public async Task A_new_run_is_allowed_once_the_previous_one_has_finished()
    {
        // The index filter must exclude completed runs, otherwise re-reviewing
        // after a fix would be impossible.
        await using AppDbContext context = NewContext();

        (Guid pullRequestId, Guid repositoryId, Guid userId, string headSha) = await SeedPullRequestAsync(context);

        ReviewRun first = TestData.QueuedRun(pullRequestId, repositoryId, userId, headSha);
        context.ReviewRuns.Add(first);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        first.MarkRunning(DateTimeOffset.UtcNow);
        first.MarkSucceeded(42, RiskBand.Moderate, "{}", "No blocking issues.", DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ReviewRuns.Add(TestData.QueuedRun(pullRequestId, repositoryId, userId, headSha));

        await Should.NotThrowAsync(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Disconnecting_a_repository_hides_it_without_destroying_review_history()
    {
        await using AppDbContext context = NewContext();

        User user = TestData.User();
        CodeRepository repository = TestData.Repository(user.Id);
        context.Users.Add(user);
        context.CodeRepositories.Add(repository);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        repository.Disconnect(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ChangeTracker.Clear();

        (await context.CodeRepositories.AnyAsync(r => r.Id == repository.Id, TestContext.Current.CancellationToken))
            .ShouldBeFalse("the global query filter should hide disconnected repositories");

        (await context.CodeRepositories.IgnoreQueryFilters()
                .AnyAsync(r => r.Id == repository.Id, TestContext.Current.CancellationToken))
            .ShouldBeTrue("the row must survive so review history remains intact");
    }

    [Fact]
    public async Task Audit_timestamps_are_written_by_the_interceptor_not_by_callers()
    {
        await using AppDbContext context = NewContext();

        User user = TestData.User();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        user.CreatedAtUtc.ShouldNotBe(default);
        user.UpdatedAtUtc.ShouldBe(user.CreatedAtUtc);

        DateTimeOffset createdAt = user.CreatedAtUtc;

        await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        user.RecordSignIn(user.Login, user.Email, user.AvatarUrl, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        user.CreatedAtUtc.ShouldBe(createdAt, "created timestamps must never move");
        user.UpdatedAtUtc.ShouldBeGreaterThan(createdAt);
    }

    [Fact]
    public async Task Enums_are_stored_as_readable_text_rather_than_ordinals()
    {
        // Ordinals would silently reinterpret every existing row the day someone
        // inserts a new member in the middle of the enum.
        await using AppDbContext context = NewContext();

        (Guid pullRequestId, Guid repositoryId, Guid userId, string headSha) = await SeedPullRequestAsync(context);

        context.ReviewRuns.Add(TestData.QueuedRun(pullRequestId, repositoryId, userId, headSha));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        List<string> statuses = await context.Database
            .SqlQuery<string>($"SELECT status FROM review_runs WHERE pull_request_id = {pullRequestId}")
            .ToListAsync(TestContext.Current.CancellationToken);

        statuses.ShouldContain(nameof(ReviewRunStatus.Queued));
    }

    private static async Task<(Guid PullRequestId, Guid RepositoryId, Guid UserId, string HeadSha)>
        SeedPullRequestAsync(AppDbContext context)
    {
        User user = TestData.User();
        CodeRepository repository = TestData.Repository(user.Id);
        string headSha = TestData.CommitSha();
        PullRequest pullRequest = TestData.PullRequest(repository.Id, headSha);

        context.Users.Add(user);
        context.CodeRepositories.Add(repository);
        context.PullRequests.Add(pullRequest);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (pullRequest.Id, repository.Id, user.Id, headSha);
    }
}
