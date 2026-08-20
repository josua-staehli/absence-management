using Common.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Common.Api;

/// <summary>
///     Single place where a business <see cref="Error" /> becomes an HTTP response.
///     Failures are returned as RFC 9457 problem details, so the frontend can display them
///     uniformly for every module, without repeating the mapping.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null)
    {
        return result.IsSuccess
            ? onSuccess?.Invoke() ?? Results.NoContent()
            : Problem(result.Error);
    }

    public static IResult ToHttpResult<TValue>(this Result<TValue> result,
        Func<TValue, IResult>? onSuccess = null)
    {
        return result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value)
            : Problem(result.Error);
    }

    /// <summary>
    ///     Declares the three failure responses <see cref="Problem" /> can produce. Endpoints return
    ///     <see cref="IResult" />, so ASP.NET Core cannot infer them - and without them the OpenAPI
    ///     document, and therefore the generated frontend client, would not know the error shape.
    ///     Pass the status codes an endpoint can actually return.
    /// </summary>
    public static RouteHandlerBuilder ProducesProblems(
        this RouteHandlerBuilder builder,
        params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes) builder.ProducesProblem(statusCode);

        return builder;
    }

    private static IResult Problem(Error error)
    {
        return Results.Problem(
            title: error.Type switch
            {
                ErrorType.NotFound => "Not found",
                ErrorType.Conflict => "Conflict",
                _ => "Invalid request"
            },
            detail: error.Message,
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            },
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
