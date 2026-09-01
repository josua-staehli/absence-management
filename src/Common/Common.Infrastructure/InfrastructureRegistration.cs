using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure;

public static class InfrastructureRegistration
{
    /// <summary>
    ///     Registers the <c>DbContext</c> of a bounded context with the connection and provider
    ///     settings that are the same for every one of them. This method decides which database is
    ///     used.
    /// </summary>
    public static IServiceCollection AddBoundedContextDbContext<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : DbContext
    {
        return services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
    }
}
