using System.Reflection;
using AiCodeReview.Application.Abstractions.Persistence;
using AiCodeReview.Domain.Ai;
using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.Jobs;
using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace AiCodeReview.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public DbSet<User> Users => Set<User>();

    public DbSet<GitHubAccount> GitHubAccounts => Set<GitHubAccount>();

    public DbSet<CodeRepository> CodeRepositories => Set<CodeRepository>();

    public DbSet<PullRequest> PullRequests => Set<PullRequest>();

    public DbSet<PullRequestFile> PullRequestFiles => Set<PullRequestFile>();

    public DbSet<ReviewRun> ReviewRuns => Set<ReviewRun>();

    public DbSet<ReviewFinding> ReviewFindings => Set<ReviewFinding>();

    public DbSet<AiUsage> AiUsages => Set<AiUsage>();

    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
