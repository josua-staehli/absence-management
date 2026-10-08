using Common.Domain.Primitives;

namespace Common.Application.Handlers;

/// <summary>
///     A reaction to a domain event of the same bounded context. No use case calls it: the unit of
///     work does, while it saves the change that raised the event and before it commits. Whatever
///     the handler changes is therefore written in the same transaction - both are stored, or
///     neither is.
///     <para>
///         That timing sets the rules for a handler. It changes data through the repositories of
///         its own bounded context and never saves, the save it runs in does that. It does not
///         reach anything that cannot roll back with the transaction, such as the database of
///         another bounded context or an e-mail. And it is no place for a business rule: a handler
///         that throws fails the whole use case with an exception, while a broken rule is an
///         <c>Error</c> returned by the aggregate or the use case.
///     </para>
///     <para>
///         A handler handles one concrete event. Events reach the handlers of their exact type, so
///         a handler for <c>IDomainEvent</c> or a base class would never be called, and
///         registering one fails on startup.
///     </para>
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
