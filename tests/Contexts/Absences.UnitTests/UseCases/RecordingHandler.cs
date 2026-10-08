using Common.Application.Handlers;
using Common.Domain.Primitives;

namespace Absences.UnitTests.UseCases;

/// <summary>
///     Hears every domain event the unit of work dispatches, through the same interface a real
///     handler implements, and keeps it. Registered once for all event types, so a new event needs
///     no change here.
/// </summary>
internal sealed class RecordingHandler<TDomainEvent>(List<IDomainEvent> dispatched)
    : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public Task HandleAsync(TDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        dispatched.Add(domainEvent);

        return Task.CompletedTask;
    }
}
