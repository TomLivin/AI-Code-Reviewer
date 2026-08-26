using AiCodeReview.Domain.PullRequests;
using AiCodeReview.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class PullRequestConfiguration : IEntityTypeConfiguration<PullRequest>
{
    public void Configure(EntityTypeBuilder<PullRequest> builder)
    {
        builder.ToTable("pull_requests");
        builder.ConfigureEntityDefaults();

        builder.Property(pullRequest => pullRequest.GitHubPullRequestId).HasColumnName("github_pull_request_id").IsRequired();
        builder.Property(pullRequest => pullRequest.GitHubUpdatedAtUtc).HasColumnName("github_updated_at_utc").IsRequired();
        builder.Property(pullRequest => pullRequest.Title).HasMaxLength(ColumnLengths.Title).IsRequired();
        builder.Property(pullRequest => pullRequest.AuthorLogin).HasMaxLength(ColumnLengths.GitHubLogin).IsRequired();
        builder.Property(pullRequest => pullRequest.State).HasEnumConversion();
        builder.Property(pullRequest => pullRequest.HeadSha).HasMaxLength(ColumnLengths.CommitSha).IsRequired();
        builder.Property(pullRequest => pullRequest.BaseSha).HasMaxLength(ColumnLengths.CommitSha).IsRequired();
        builder.Property(pullRequest => pullRequest.HeadRef).HasMaxLength(ColumnLengths.GitRef).IsRequired();
        builder.Property(pullRequest => pullRequest.BaseRef).HasMaxLength(ColumnLengths.GitRef).IsRequired();

        // Natural key, which turns synchronisation into an idempotent upsert.
        builder.HasIndex(pullRequest => new { pullRequest.CodeRepositoryId, pullRequest.Number })
            .IsUnique();

        // Backs the default listing: most recently updated first, per repository.
        builder.HasIndex(pullRequest => new { pullRequest.CodeRepositoryId, pullRequest.GitHubUpdatedAtUtc })
            .IsDescending(false, true);

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(pullRequest => pullRequest.CodeRepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pullRequest => pullRequest.Files)
            .WithOne()
            .HasForeignKey(file => file.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(pullRequest => pullRequest.Files)
            .HasField("_files")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
