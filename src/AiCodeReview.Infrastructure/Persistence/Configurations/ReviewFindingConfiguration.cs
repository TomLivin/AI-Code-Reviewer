using AiCodeReview.Domain.Identity;
using AiCodeReview.Domain.Repositories;
using AiCodeReview.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class ReviewFindingConfiguration : IEntityTypeConfiguration<ReviewFinding>
{
    public void Configure(EntityTypeBuilder<ReviewFinding> builder)
    {
        builder.ToTable("review_findings");
        builder.ConfigureEntityDefaults();

        builder.Property(finding => finding.Source).HasEnumConversion();
        builder.Property(finding => finding.Category).HasEnumConversion();
        builder.Property(finding => finding.Severity).HasEnumConversion();
        builder.Property(finding => finding.Confidence).HasEnumConversion();
        builder.Property(finding => finding.Status).HasEnumConversion();

        builder.Property(finding => finding.RuleCode).HasMaxLength(ColumnLengths.RuleCode);
        builder.Property(finding => finding.Title).HasMaxLength(ColumnLengths.Title).IsRequired();
        builder.Property(finding => finding.FilePath).HasMaxLength(ColumnLengths.FilePath).IsRequired();
        builder.Property(finding => finding.Fingerprint).HasMaxLength(ColumnLengths.Fingerprint).IsRequired();
        builder.Property(finding => finding.SuppressionReason).HasMaxLength(ColumnLengths.SuppressionReason);

        builder.Property(finding => finding.Description).HasColumnType("text").IsRequired();
        builder.Property(finding => finding.Reasoning).HasColumnType("text");
        builder.Property(finding => finding.Recommendation).HasColumnType("text");
        builder.Property(finding => finding.SuggestedFix).HasColumnType("text");

        // De-duplication enforced by the database, not only by the merge step.
        // If that logic ever regresses, this catches it instead of silently
        // showing the user the same problem twice.
        builder.HasIndex(finding => new { finding.ReviewRunId, finding.Fingerprint })
            .IsUnique();

        // The analytics query: findings by severity over time for a repository.
        // Serving it from one index is the entire reason repository id is
        // denormalised onto this table.
        builder.HasIndex(finding => new { finding.CodeRepositoryId, finding.Severity, finding.CreatedAtUtc })
            .IsDescending(false, false, true);

        // Matching a candidate finding against ones already dismissed here.
        builder.HasIndex(finding => new { finding.CodeRepositoryId, finding.Fingerprint });

        builder.HasOne<CodeRepository>()
            .WithMany()
            .HasForeignKey(finding => finding.CodeRepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(finding => finding.StatusChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
