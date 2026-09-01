using Common.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Api;

/// <summary>
///     What adding a bounded context to the host comes down to, for every one of them: read the
///     connection string it owns, register the use cases of its application assembly, then hand
///     the string to its infrastructure. A bounded context's own registration is therefore left
///     with the two things that actually differ between them.
/// </summary>
public static class BoundedContextRegistration
{
    /// <typeparam name="TMarker">
    ///     Any type of the bounded context's application assembly. Every command and query handler
    ///     next to it is registered, so a new use case needs no change here.
    /// </typeparam>
    /// <param name="connectionStringName">
    ///     Name of the connection string this bounded context reads, and of the database it owns.
    /// </param>
    /// <param name="addInfrastructure">
    ///     The bounded context's own infrastructure registration: its <c>DbContext</c>,
    ///     repositories, queries and database initializer.
    /// </param>
    public static IHostApplicationBuilder AddBoundedContext<TMarker>(
        this IHostApplicationBuilder builder,
        string connectionStringName,
        Action<IServiceCollection, string> addInfrastructure)
    {
        // Fail fast and with a readable message: a missing connection string otherwise only shows
        // up when the first request hits the database.
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{connectionStringName}' is missing. Start "
                                   + "the solution with the Aspire AppHost or set "
                                   + $"ConnectionStrings__{connectionStringName}.");

        builder.Services.AddHandlersFromAssemblyOf<TMarker>();
        addInfrastructure(builder.Services, connectionString);

        return builder;
    }
}
