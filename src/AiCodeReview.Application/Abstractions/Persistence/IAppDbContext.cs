using AiCodeReview.Domain.Ai;
using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.Jobs;
using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace AiCodeReview.Application.Abstractions.Persistence;

/// <summary>
/// The persistence port. Deliberately exposes <see cref="DbSet{TEntity}"/>
/// rather than a repository per aggregate: read handlers project straight to
/// DTOs with LINQ, and wrapping that in generic repositories would add a layer
/// that only forwards calls. See ADR-010 for the trade-off.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<GitHubAccount> GitHubAccounts { get; }

    DbSet<CodeRepository> CodeRepositories { get; }

    DbSet<PullRequest> PullRequests { get; }

    DbSet<PullRequestFile> PullRequestFiles { get; }

    DbSet<ReviewRun> ReviewRuns { get; }

    DbSet<ReviewFinding> ReviewFindings { get; }

    DbSet<AiUsage> AiUsages { get; }

    DbSet<BackgroundJob> BackgroundJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
