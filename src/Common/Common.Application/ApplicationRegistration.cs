using System.Reflection;
using Common.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Application;

public static class ApplicationRegistration
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>)
    ];

    /// <summary>
    ///     Registers every command, query and domain event handler of the assembly that contains
    ///     <typeparamref name="TMarker" />. A new use case or a new reaction to an event therefore
    ///     only needs a new class - no change to the composition root, and no change when a bounded
    ///     context is added.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssemblyOf<TMarker>(
        this IServiceCollection services)
    {
        return services.AddHandlersFromAssembly(typeof(TMarker).Assembly);
    }

    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services,
        Assembly assembly)
    {
        var handlers = assembly
            .GetTypes()
            .Where(type => type is
                { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var handler in handlers)
        {
            var implementedHandlerInterfaces = handler.GetInterfaces()
                .Where(@interface => @interface.IsGenericType
                                     && HandlerInterfaces.Contains(
                                         @interface.GetGenericTypeDefinition()));

            foreach (var @interface in implementedHandlerInterfaces)
            {
                EnsureEventHandlerCanBeCalled(handler, @interface);
                services.AddScoped(@interface, handler);
            }
        }

        // Handlers take the current time as a dependency so tests can control it.
        services.TryAddTimeProvider();

        return services;
    }

    /// <summary>
    ///     A domain event reaches the handlers of its exact type. A handler for an interface or a
    ///     base class - <c>IDomainEvent</c>, say, to hear every event - would compile and register,
    ///     and then never be called. It is refused here, on startup, rather than left to be silent.
    /// </summary>
    private static void EnsureEventHandlerCanBeCalled(Type handler, Type handlerInterface)
    {
        if (handlerInterface.GetGenericTypeDefinition() != typeof(IDomainEventHandler<>)) return;

        var eventType = handlerInterface.GenericTypeArguments[0];
        if (!eventType.IsSealed)
            throw new InvalidOperationException(
                $"{handler.Name} handles {eventType.Name}, which no event is dispatched as. A "
                + "domain event handler handles one concrete event, a sealed record.");
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}
