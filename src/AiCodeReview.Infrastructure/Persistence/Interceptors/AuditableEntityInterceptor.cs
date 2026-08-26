using AiCodeReview.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AiCodeReview.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit timestamps centrally. Doing this in an interceptor rather than
/// in each entity means no call site can forget, and no entity needs a public
/// setter on a field callers must not control.
/// </summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            // An owned or unchanged entity whose child collection changed still
            // counts as modified for audit purposes.
            bool touched = entry.State is EntityState.Added or EntityState.Modified
                || entry.References.Any(reference => reference.TargetEntry?.State is EntityState.Added or EntityState.Modified);

            if (!touched)
            {
                continue;
            }

            if (entry.State is EntityState.Added)
            {
                entry.Property(nameof(Entity.CreatedAtUtc)).CurrentValue = now;
            }

            entry.Property(nameof(Entity.UpdatedAtUtc)).CurrentValue = now;
        }
    }
}
