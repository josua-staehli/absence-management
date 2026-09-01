using Absences.Api.Endpoints;
using Absences.Application;
using Absences.Infrastructure;
using Common.Api;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Absences.Api;

/// <summary>
///     The seam between the host and this bounded context: two methods, one for the services and
///     one for the routes. The host knows nothing else about it - not the handlers, not the
///     <c>DbContext</c>, not the endpoints.
/// </summary>
public static class AbsencesBoundedContext
{
    /// <summary>
    ///     Name of the connection string this bounded context reads from the configuration.
    /// </summary>
    public const string ConnectionStringName = "absencedb";

    public static IHostApplicationBuilder AddAbsencesBoundedContext(
        this IHostApplicationBuilder builder)
    {
        return builder.AddBoundedContext<CreateAbsenceRequestCommand>(
            ConnectionStringName,
            (services, connectionString) => services.AddAbsencesInfrastructure(connectionString));
    }

    public static IEndpointRouteBuilder MapAbsencesBoundedContext(this IEndpointRouteBuilder app)
    {
        app.MapAbsenceRequestEndpoints();

        return app;
    }
}
