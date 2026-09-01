using Common.Api;
using Employees.Api.Endpoints;
using Employees.Application;
using Employees.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Employees.Api;

/// <summary>
///     The seam between the host and this bounded context: two methods, one for the services and
///     one for the routes. The host knows nothing else about it - not the handlers, not the
///     <c>DbContext</c>, not the endpoints.
/// </summary>
public static class EmployeesBoundedContext
{
    /// <summary>
    ///     Name of the connection string this bounded context reads from the configuration.
    /// </summary>
    public const string ConnectionStringName = "employeedb";

    public static IHostApplicationBuilder AddEmployeesBoundedContext(
        this IHostApplicationBuilder builder)
    {
        return builder.AddBoundedContext<CreateEmployeeCommand>(
            ConnectionStringName,
            (services, connectionString) => services.AddEmployeesInfrastructure(connectionString));
    }

    public static IEndpointRouteBuilder MapEmployeesBoundedContext(this IEndpointRouteBuilder app)
    {
        app.MapEmployeeEndpoints();

        return app;
    }
}
