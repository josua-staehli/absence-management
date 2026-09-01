using Common.Api;
using Common.Application.Handlers;
using Employees.Api.Contracts;
using Employees.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Employees.Api.Endpoints;

/// <summary>
///     REST endpoints of the employees. They only translate HTTP to a use case and back, and hold
///     no business logic: the handler is asked for a <c>Result</c>, and
///     <see cref="ResultExtensions.ToHttpResult{TValue}" /> turns it into a response.
/// </summary>
internal static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees").WithTags("Employees");

        // The .WithName / .Produces calls are not decoration: they are what the OpenAPI document
        // is built from, and a frontend client is generated from that document. The name becomes
        // the function name in TypeScript, the types become its signature.

        group.MapGet("/", async (
                IQueryHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetEmployeesQuery(), cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("listEmployees")
            .Produces<IReadOnlyList<EmployeeDto>>();

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<GetEmployeeByIdQuery, EmployeeDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetEmployeeByIdQuery(id),
                    cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("getEmployee")
            .Produces<EmployeeDto>()
            .ProducesProblems(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateEmployeeRequest request,
                ICommandHandler<CreateEmployeeCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateEmployeeCommand(
                    request.FirstName,
                    request.LastName,
                    request.Email);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.ToHttpResult(id => Results.Created(
                    $"/api/employees/{id}",
                    new CreateEmployeeResponse(id)));
            })
            .WithName("createEmployee")
            .Produces<CreateEmployeeResponse>(StatusCodes.Status201Created)
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status409Conflict);

        return app;
    }
}
