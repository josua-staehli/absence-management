using Common.Application;
using Common.Application.Handlers;
using Common.Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Common.UnitTests.Application;

/// <summary>
///     The assembly scan that registers the handlers of a bounded context.
/// </summary>
public class ApplicationRegistrationTests
{
    /// <summary>
    ///     A domain event reaches the handlers of its exact type, so a handler for every event
    ///     would register fine and then never be called. The scan refuses it instead - which also
    ///     shows that it finds domain event handlers at all, or there would be nothing to refuse.
    /// </summary>
    [Fact]
    public void A_handler_for_every_event_is_refused_when_it_is_registered()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddHandlersFromAssemblyOf<ApplicationRegistrationTests>());

        Assert.Contains(nameof(EveryEventHandler), exception.Message);
    }
}

/// <summary>
///     Would hear every event, if events were dispatched to the handlers of their interfaces.
///     Being part of this assembly, it makes every scan of it fail - see the test above.
/// </summary>
internal sealed class EveryEventHandler : IDomainEventHandler<IDomainEvent>
{
    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
