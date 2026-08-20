namespace Common.Domain.Results;

/// <summary>
///     Classifies an <see cref="Error" /> so that outer layers (e.g. the API) can translate it
///     into a transport-specific result without knowing every error code.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

/// <summary>
///     A business error. Errors are values, not exceptions: expected failures travel through
///     <see cref="Result" /> instead of the exception pipeline.
/// </summary>
/// <param name="Code">Stable, machine-readable identifier, e.g. <c>MyModule.OverlappingDate</c>.</param>
/// <param name="Message">Human-readable description, shown in the UI.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message)
    {
        return new Error(code, message, ErrorType.Validation);
    }

    public static Error NotFound(string code, string message)
    {
        return new Error(code, message, ErrorType.NotFound);
    }

    public static Error Conflict(string code, string message)
    {
        return new Error(code, message, ErrorType.Conflict);
    }
}
