namespace Common.Domain.Primitives;

/// <summary>
///     Entry point of an aggregate. Only aggregate roots are loaded and stored via a repository,
///     which keeps the transactional boundary explicit.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }
}
