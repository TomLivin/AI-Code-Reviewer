using AiCodeReview.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class GitHubAccountConfiguration : IEntityTypeConfiguration<GitHubAccount>
{
    public void Configure(EntityTypeBuilder<GitHubAccount> builder)
    {
        builder.ToTable("github_accounts");
        builder.ConfigureEntityDefaults();

        builder.Property(account => account.AccessTokenProtected).IsRequired();
        builder.Property(account => account.RefreshTokenProtected);
        builder.Property(account => account.Scopes).IsRequired();
        builder.Property(account => account.ConnectedAtUtc).IsRequired();

        // One GitHub connection per user, enforced by the database rather than
        // by an application check that a concurrent callback could slip past.
        builder.HasIndex(account => account.UserId).IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<GitHubAccount>(account => account.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
