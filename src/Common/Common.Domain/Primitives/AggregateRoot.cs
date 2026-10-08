namespace Common.Domain.Primitives;

/// <summary>
///     What the unit of work needs to know about an aggregate root without knowing the type of its
///     id: the domain events it raised since it was last saved.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
///     Entry point of an aggregate. Only aggregate roots are loaded and stored via a repository,
///     which keeps the transactional boundary explicit.
///     <para>
///         It is also the only kind of object that raises domain events. The change and the event
///         that reports it are made by the same method, so they cannot drift apart.
///     </para>
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    ///     The events raised since the aggregate was last saved, in the order they happened.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Called by the unit of work once it has taken the events for dispatching.</summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    ///     Records that something happened to this aggregate. Nothing reacts to it yet: the event
    ///     is dispatched when the unit of work saves the change, and a change that is never saved
    ///     is never heard of.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
