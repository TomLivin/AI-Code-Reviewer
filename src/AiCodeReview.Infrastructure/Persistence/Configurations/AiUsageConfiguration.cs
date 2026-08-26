using AiCodeReview.Domain.Ai;
using AiCodeReview.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.ToTable("ai_usages");
        builder.ConfigureEntityDefaults();

        builder.Property(usage => usage.Stage).HasMaxLength(ColumnLengths.PipelineStage).IsRequired();
        builder.Property(usage => usage.Provider).HasMaxLength(ColumnLengths.ProviderName).IsRequired();
        builder.Property(usage => usage.Model).HasMaxLength(ColumnLengths.ModelName).IsRequired();

        // Fixed-point, not floating point. Per-call costs are fractions of a
        // cent and summing binary floats across thousands of rows accumulates
        // error in a number the user is shown as money.
        builder.Property(usage => usage.EstimatedCostUsd)
            .HasColumnType("numeric(12,6)")
            .IsRequired();

        builder.HasIndex(usage => usage.ReviewRunId);

        // Cost reporting over a period, without scanning the table.
        builder.HasIndex(usage => usage.CreatedAtUtc);

        builder.HasOne<ReviewRun>()
            .WithMany()
            .HasForeignKey(usage => usage.ReviewRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
