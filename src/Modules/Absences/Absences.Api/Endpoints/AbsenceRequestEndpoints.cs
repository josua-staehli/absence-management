using Absences.Api.Contracts;
using Absences.Application;
using Common.Api;
using Common.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Absences.Api.Endpoints;

/// <summary>
///     REST endpoints of the absence requests. They only translate HTTP to a use case and back, and
///     hold no business logic: the handler is asked for a <c>Result</c>, and
///     <see cref="ResultExtensions.ToHttpResult{TValue}" /> turns it into a response.
/// </summary>
internal static class AbsenceRequestEndpoints
{
    public static IEndpointRouteBuilder MapAbsenceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/absence-requests").WithTags("AbsenceRequests");

        // The .WithName / .Produces calls are not decoration: they are what the OpenAPI document
        // is built from, and a frontend client is generated from that document. The name becomes
        // the function name in TypeScript, the types become its signature.

        group.MapGet("/", async (
                IQueryHandler<GetAbsenceRequestsQuery, IReadOnlyList<AbsenceRequestDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetAbsenceRequestsQuery(),
                    cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("listAbsenceRequests")
            .Produces<IReadOnlyList<AbsenceRequestDto>>();

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<GetAbsenceRequestByIdQuery, AbsenceRequestDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetAbsenceRequestByIdQuery(id),
                    cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("getAbsenceRequest")
            .Produces<AbsenceRequestDto>()
            .ProducesProblems(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateAbsenceRequestRequest request,
                ICommandHandler<CreateAbsenceRequestCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateAbsenceRequestCommand(
                    request.EmployeeId,
                    request.Type,
                    request.StartDate,
                    request.EndDate,
                    request.Comment);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.ToHttpResult(id => Results.Created(
                    $"/api/absence-requests/{id}",
                    new CreateAbsenceRequestResponse(id)));
            })
            .WithName("createAbsenceRequest")
            .Produces<CreateAbsenceRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateAbsenceRequestRequest request,
                ICommandHandler<UpdateAbsenceRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateAbsenceRequestCommand(
                    id,
                    request.Type,
                    request.StartDate,
                    request.EndDate,
                    request.Comment);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("updateAbsenceRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/approve", async (
                Guid id,
                ICommandHandler<ApproveAbsenceRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ApproveAbsenceRequestCommand(id),
                    cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("approveAbsenceRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", async (
                Guid id,
                ICommandHandler<RejectAbsenceRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RejectAbsenceRequestCommand(id),
                    cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("rejectAbsenceRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);

        return app;
    }
}
