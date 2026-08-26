using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class CodeRepositoryConfiguration : IEntityTypeConfiguration<CodeRepository>
{
    public void Configure(EntityTypeBuilder<CodeRepository> builder)
    {
        builder.ToTable("code_repositories");
        builder.ConfigureEntityDefaults();

        builder.Property(repository => repository.GitHubRepositoryId).HasColumnName("github_repository_id").IsRequired();
        builder.Property(repository => repository.FullName).HasMaxLength(ColumnLengths.RepositoryFullName).IsRequired();
        builder.Property(repository => repository.DefaultBranch).HasMaxLength(ColumnLengths.GitRef).IsRequired();
        builder.Property(repository => repository.PrimaryLanguage).HasMaxLength(ColumnLengths.GitHubLogin);

        builder.Ignore(repository => repository.IsConnected);

        // Uniqueness deliberately ignores the soft-delete flag: reconnecting a
        // repository must restore the existing row, not insert a second one.
        builder.HasIndex(repository => new { repository.OwnerUserId, repository.GitHubRepositoryId })
            .IsUnique();

        // Every listing filters out disconnected repositories, so a partial
        // index keeps those rows out of the index altogether.
        builder.HasIndex(repository => repository.OwnerUserId)
            .HasFilter("deleted_at_utc IS NULL");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(repository => repository.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Disconnecting hides the repository without destroying review history.
        builder.HasQueryFilter(repository => repository.DeletedAtUtc == null);
    }
}
