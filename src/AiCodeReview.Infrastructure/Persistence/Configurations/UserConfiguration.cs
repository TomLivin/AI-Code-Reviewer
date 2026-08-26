using AiCodeReview.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureEntityDefaults();

        // The convention splits "GitHub" into "git_hub"; named explicitly so the
        // column matches the github_accounts table and any hand-written SQL.
        builder.Property(user => user.GitHubUserId).HasColumnName("github_user_id").IsRequired();
        builder.Property(user => user.Login).HasMaxLength(ColumnLengths.GitHubLogin).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(ColumnLengths.Email);
        builder.Property(user => user.AvatarUrl).HasMaxLength(ColumnLengths.Url);

        // Keyed on the numeric id rather than the login, because a GitHub login
        // can be renamed and later reused by someone else. This is the lookup
        // the OAuth callback performs on every sign-in.
        builder.HasIndex(user => user.GitHubUserId).IsUnique();
    }
}
