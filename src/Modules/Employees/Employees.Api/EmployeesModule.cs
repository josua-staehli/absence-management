using Common.Api;
using Employees.Api.Endpoints;
using Employees.Application;
using Employees.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Employees.Api;

/// <summary>
///     The seam between the host and this module: two methods, one for the services and one for the
///     routes. The host knows nothing else about the module - not the handlers, not the
///     <c>DbContext</c>, not the endpoints.
/// </summary>
public static class EmployeesModule
{
    /// <summary>Name of the connection string this module reads from the configuration.</summary>
    public const string ConnectionStringName = "employeedb";

    public static IHostApplicationBuilder AddEmployeesModule(this IHostApplicationBuilder builder)
    {
        return builder.AddModule<CreateEmployeeCommand>(
            ConnectionStringName,
            (services, connectionString) => services.AddEmployeesInfrastructure(connectionString));
    }

    public static IEndpointRouteBuilder MapEmployeesModule(this IEndpointRouteBuilder app)
    {
        app.MapEmployeeEndpoints();

        return app;
    }
}
