using AiCodeReview.Domain.PullRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class PullRequestFileConfiguration : IEntityTypeConfiguration<PullRequestFile>
{
    public void Configure(EntityTypeBuilder<PullRequestFile> builder)
    {
        builder.ToTable("pull_request_files");
        builder.ConfigureEntityDefaults();

        builder.Property(file => file.HeadSha).HasMaxLength(ColumnLengths.CommitSha).IsRequired();
        builder.Property(file => file.Path).HasMaxLength(ColumnLengths.FilePath).IsRequired();
        builder.Property(file => file.PreviousPath).HasMaxLength(ColumnLengths.FilePath);
        builder.Property(file => file.ChangeStatus).HasEnumConversion();
        builder.Property(file => file.BlobSha).HasMaxLength(ColumnLengths.CommitSha);
        builder.Property(file => file.Patch);

        builder.Ignore(file => file.IsAnalysable);

        // Snapshots are keyed by commit, so a new push adds rows rather than
        // overwriting the state an earlier review was based on.
        builder.HasIndex(file => new { file.PullRequestId, file.HeadSha, file.Path })
            .IsUnique();
    }
}
