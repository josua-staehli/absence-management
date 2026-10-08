using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure;

public static class InfrastructureRegistration
{
    /// <summary>
    ///     Registers the <c>DbContext</c> of a bounded context with the connection and provider
    ///     settings that are the same for every one of them. This method decides which database is
    ///     used, and that saving dispatches the domain events of the saved aggregates first.
    /// </summary>
    public static IServiceCollection AddBoundedContextDbContext<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : DbContext
    {
        // The service provider handed in here is the scope the DbContext is created for, so the
        // interceptor resolves the handlers from the same scope - and they write through the same
        // DbContext instance, inside the same transaction. A new interceptor per scope is cheap:
        // interceptors belong to the context instance, so EF Core still builds its own internal
        // service provider only once.
        return services.AddDbContext<TContext>((serviceProvider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .AddInterceptors(new DispatchDomainEventsInterceptor(serviceProvider)));
    }
}
