namespace AiCodeReview.Domain.Common;

/// <summary>
/// Base type for persisted entities. Audit timestamps are written by a
/// persistence interceptor rather than by callers, so they expose no public
/// setter; EF Core assigns them through the backing field.
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    protected Entity()
    {
    }

    public Guid Id { get; protected init; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id && Id != Guid.Empty;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
