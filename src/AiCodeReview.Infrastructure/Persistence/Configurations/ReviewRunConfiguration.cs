using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class ReviewRunConfiguration : IEntityTypeConfiguration<ReviewRun>
{
    /// <summary>
    /// Values of <see cref="ReviewRunStatus"/> that count as in-flight. Kept as
    /// a literal because it is embedded in an index filter, which PostgreSQL
    /// stores as SQL text; changing the enum names means a new migration.
    /// </summary>
    private const string ActiveStatusFilter = "status IN ('Queued', 'Running')";

    public void Configure(EntityTypeBuilder<ReviewRun> builder)
    {
        builder.ToTable("review_runs");
        builder.ConfigureEntityDefaults();

        builder.Property(run => run.HeadSha).HasMaxLength(ColumnLengths.CommitSha).IsRequired();
        builder.Property(run => run.Status).HasEnumConversion();
        builder.Property(run => run.Trigger).HasEnumConversion();
        builder.Property(run => run.RiskBand).HasEnumConversion();
        builder.Property(run => run.ErrorCode).HasMaxLength(ColumnLengths.ErrorCode);
        builder.Property(run => run.ErrorMessage).HasMaxLength(ColumnLengths.ErrorMessage);
        builder.Property(run => run.CorrelationId).IsRequired();

        // The itemised score explanation is queried as a whole, never filtered
        // on, so jsonb buys nothing over text here and text avoids the parse
        // cost on write.
        builder.Property(run => run.ScoreBreakdownJson).HasColumnType("text");
        builder.Property(run => run.Summary).HasColumnType("text");

        builder.Ignore(run => run.IsActive);

        // Concurrency control expressed in the schema: a partial unique index
        // makes it impossible to have two in-flight runs for the same commit.
        // A double-clicked Review button hits a 23505 the API turns into the
        // existing run, so the second click costs nothing instead of a second
        // set of model calls. Completed runs are excluded, so rerunning after a
        // finished review still works.
        builder.HasIndex(run => new { run.PullRequestId, run.HeadSha })
            .IsUnique()
            .HasFilter(ActiveStatusFilter)
            .HasDatabaseName("ix_review_runs_active_per_commit");

        // Review history for a pull request, newest first.
        builder.HasIndex(run => new { run.PullRequestId, run.QueuedAtUtc })
            .IsDescending(false, true);

        builder.HasOne<PullRequest>()
            .WithMany()
            .HasForeignKey(run => run.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(run => run.CodeRepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user with review history cannot be hard-deleted; the correct
        // response to an erasure request is anonymisation, not silently
        // destroying other people's review records.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(run => run.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(run => run.Findings)
            .WithOne()
            .HasForeignKey(finding => finding.ReviewRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(run => run.Findings)
            .HasField("_findings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
