using Absences.Api.Endpoints;
using Absences.Application;
using Absences.Infrastructure;
using Common.Api;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Absences.Api;

/// <summary>
///     The seam between the host and this module: two methods, one for the services and one for the
///     routes. The host knows nothing else about the module - not the handlers, not the
///     <c>DbContext</c>, not the endpoints.
/// </summary>
public static class AbsencesModule
{
    /// <summary>Name of the connection string this module reads from the configuration.</summary>
    public const string ConnectionStringName = "absencedb";

    public static IHostApplicationBuilder AddAbsencesModule(this IHostApplicationBuilder builder)
    {
        return builder.AddModule<CreateAbsenceRequestCommand>(
            ConnectionStringName,
            (services, connectionString) => services.AddAbsencesInfrastructure(connectionString));
    }

    public static IEndpointRouteBuilder MapAbsencesModule(this IEndpointRouteBuilder app)
    {
        app.MapAbsenceRequestEndpoints();

        return app;
    }
}
