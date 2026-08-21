using Common.Api;
using Common.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Common.UnitTests.Api;

/// <summary>
///     The single translation from a business <see cref="Error" /> to an HTTP response. Every
///     module goes through it, so an endpoint never picks a status code of its own - and the
///     frontend reads what comes out of it. Pinned here rather than in the endpoints that use it.
/// </summary>
public class ResultExtensionsTests
{
    private static readonly Error ValidationError =
        Error.Validation("Tests.Invalid", "The request is not valid.");

    private static readonly Error NotFoundError =
        Error.NotFound("Tests.NotFound", "No such thing exists.");

    private static readonly Error ConflictError =
        Error.Conflict("Tests.Conflict", "That collides with something else.");

    /// <summary>
    ///     The three error types and the responses they become. This is the whole reason
    ///     <see cref="ErrorType" /> exists: an outer layer maps the classification, not the code.
    /// </summary>
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest, "Invalid request")]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, "Not found")]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, "Conflict")]
    public void An_error_becomes_the_status_and_the_title_of_its_type(
        ErrorType type,
        int expectedStatusCode,
        string expectedTitle)
    {
        var response = Result.Failure(ErrorOfType(type)).ToHttpResult();

        var problem = Assert.IsType<ProblemHttpResult>(response);
        Assert.Equal(expectedStatusCode, problem.StatusCode);
        Assert.Equal(expectedStatusCode, problem.ProblemDetails.Status);
        Assert.Equal(expectedTitle, problem.ProblemDetails.Title);
    }

    /// <summary>
    ///     What the frontend reads from a failure: the message the domain wrote, and the stable
    ///     code as an RFC 9457 extension member. The member is called <c>code</c> in
    ///     <c>shared/api-client/src/lib/client.ts</c>. It appears in no generated type, so nothing
    ///     but this test keeps the two names together.
    /// </summary>
    [Fact]
    public void A_failure_carries_the_message_and_the_stable_code()
    {
        var response = Result.Failure(ConflictError).ToHttpResult();

        var problem = Assert.IsType<ProblemHttpResult>(response);
        Assert.Equal(ConflictError.Message, problem.ProblemDetails.Detail);
        Assert.True(problem.ProblemDetails.Extensions.TryGetValue("code", out var code));
        Assert.Equal(ConflictError.Code, code);
    }

    /// <summary>
    ///     A failed <see cref="Result{TValue}" /> has no value - reading it throws. The mapping
    ///     therefore has to look at the failure before it touches the success callback.
    /// </summary>
    [Fact]
    public void A_failure_never_reaches_the_success_callback()
    {
        Result<int> result = NotFoundError;

        var response = result.ToHttpResult(_ =>
            throw new InvalidOperationException("A failed result must not run the callback."));

        var problem = Assert.IsType<ProblemHttpResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    /// <summary>A command that answers with nothing becomes <c>204 No Content</c>.</summary>
    [Fact]
    public void A_success_without_a_value_is_no_content()
    {
        var response = Result.Success().ToHttpResult();

        Assert.IsType<NoContent>(response);
    }

    /// <summary>A query answers with its value, and the value is not wrapped in anything.</summary>
    [Fact]
    public void A_success_with_a_value_is_ok_and_carries_it()
    {
        var response = Result.Success("Anna Meier").ToHttpResult();

        var ok = Assert.IsType<Ok<string>>(response);
        Assert.Equal("Anna Meier", ok.Value);
    }

    /// <summary>
    ///     An endpoint that answers with something else says so itself - the <c>201 Created</c> of
    ///     a create, for example. Only the success side is its business, the failures stay mapped
    ///     the same way.
    /// </summary>
    [Fact]
    public void A_success_with_a_value_can_choose_its_own_response()
    {
        var id = Guid.CreateVersion7();

        var response = Result.Success(id)
            .ToHttpResult(value => Results.Created($"/api/things/{value}", value));

        var created = Assert.IsType<Created<Guid>>(response);
        Assert.Equal($"/api/things/{id}", created.Location);
        Assert.Equal(id, created.Value);
    }

    /// <summary>The same for a command: what the callback returns is passed through untouched.</summary>
    [Fact]
    public void A_success_without_a_value_can_choose_its_own_response_too()
    {
        var chosen = new ChosenResult();

        var response = Result.Success().ToHttpResult(() => chosen);

        Assert.Same(chosen, response);
    }

    private static Error ErrorOfType(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => ValidationError,
            ErrorType.NotFound => NotFoundError,
            ErrorType.Conflict => ConflictError,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                "Every error type needs a mapping of its own.")
        };
    }

    /// <summary>Stands in for whatever response an endpoint hands to <c>ToHttpResult</c>.</summary>
    private sealed class ChosenResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
    }
}
