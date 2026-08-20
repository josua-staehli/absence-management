namespace Common.Domain.Primitives;

/// <summary>
///     Base class for objects with a life cycle and an identity.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>Used by EF Core for materialization.</summary>
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; }

    public bool Equals(Entity<TId>? other)
    {
        return other is not null && other.GetType() == GetType() && other.Id.Equals(Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
