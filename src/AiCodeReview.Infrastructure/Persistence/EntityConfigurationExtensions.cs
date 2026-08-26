using AiCodeReview.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCodeReview.Infrastructure.Persistence;

internal static class EntityConfigurationExtensions
{
    /// <summary>
    /// Key and audit columns are identical for every entity, so they are
    /// configured once here rather than repeated in nine configurations.
    ///
    /// Ids are never database-generated: entities are valid the moment they are
    /// constructed, and UUIDv7 is time-ordered, so inserts append to the index
    /// instead of fragmenting it the way random UUIDv4 keys do.
    /// </summary>
    internal static void ConfigureEntityDefaults<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : Entity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAtUtc)
            .IsRequired();
    }

    /// <summary>
    /// Enums are stored as text, not ordinals: readable in psql, and adding a
    /// member never silently reinterprets existing rows the way inserting into
    /// an int-backed enum would.
    /// </summary>
    internal static PropertyBuilder<TEnum> HasEnumConversion<TEnum>(this PropertyBuilder<TEnum> builder)
        where TEnum : struct, Enum =>
        builder.HasConversion<string>().HasMaxLength(ColumnLengths.Enum).IsRequired();

    /// <summary>Nullable overload, for enum columns that are genuinely optional.</summary>
    internal static PropertyBuilder<TEnum?> HasEnumConversion<TEnum>(this PropertyBuilder<TEnum?> builder)
        where TEnum : struct, Enum =>
        builder.HasConversion<string>().HasMaxLength(ColumnLengths.Enum);
}
