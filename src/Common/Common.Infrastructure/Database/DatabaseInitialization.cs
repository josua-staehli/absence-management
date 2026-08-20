using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Database;

public static class DatabaseInitialization
{
    public static IServiceCollection AddDbInitializer<TInitializer>(
        this IServiceCollection services)
        where TInitializer : class, IDbInitializer
    {
        return services.AddScoped<IDbInitializer, TInitializer>();
    }

    /// <summary>
    ///     Runs the initializer of every registered module. The host calls this once on startup in
    ///     development.
    /// </summary>
    public static async Task InitializeModulesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
            await initializer.InitializeAsync(cancellationToken);
    }
}
