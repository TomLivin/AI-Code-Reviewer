using AiCodeReview.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence.Configurations;

internal sealed class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    private const string PendingFilter = "state = 'Pending'";

    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("background_jobs");
        builder.ConfigureEntityDefaults();

        builder.Property(job => job.Type).HasEnumConversion();
        builder.Property(job => job.State).HasEnumConversion();
        builder.Property(job => job.LockedBy).HasMaxLength(ColumnLengths.WorkerId);
        builder.Property(job => job.LastError).HasMaxLength(ColumnLengths.ErrorMessage);
        builder.Property(job => job.CorrelationId).IsRequired();

        // Payloads are read whole by the handler that owns the job type; jsonb
        // would only pay off if we queried inside them, which we do not.
        builder.Property(job => job.PayloadJson).HasColumnType("text").IsRequired();

        // The hottest query in the system: the dispatcher polls for claimable
        // work continuously. A partial index contains only pending rows, so it
        // stays small no matter how much completed history accumulates.
        builder.HasIndex(job => job.AvailableAtUtc)
            .HasFilter(PendingFilter)
            .HasDatabaseName("ix_background_jobs_claimable");

        // The reaper scans for leases that expired because a worker died.
        builder.HasIndex(job => job.LockedUntilUtc)
            .HasFilter("state = 'Running'")
            .HasDatabaseName("ix_background_jobs_expiring_leases");
    }
}
