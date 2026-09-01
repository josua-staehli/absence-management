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
        typeof(IQueryHandler<,>)
    ];

    /// <summary>
    ///     Registers every command and query handler of the assembly that contains
    ///     <typeparamref name="TMarker" />. A new use case therefore only needs a new class -
    ///     no change to the composition root, and no change when a bounded context is added.
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
                services.AddScoped(@interface, handler);
        }

        // Handlers take the current time as a dependency so tests can control it.
        services.TryAddTimeProvider();

        return services;
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}
