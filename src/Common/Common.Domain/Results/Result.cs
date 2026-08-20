namespace Common.Domain.Results;

/// <summary>
///     Outcome of an operation that can fail for business reasons.
/// </summary>
public class Result
{
    private readonly Error? _error;

    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error =>
        _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success()
    {
        return new Result(null);
    }

    public static Result Failure(Error error)
    {
        return new Result(error);
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value, null);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new Result<TValue>(default, error);
    }

    public static implicit operator Result(Error error)
    {
        return Failure(error);
    }
}

/// <summary>
///     Outcome of an operation that returns a value on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, Error? error) : base(error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<TValue>(TValue value)
    {
        return Success(value);
    }

    public static implicit operator Result<TValue>(Error error)
    {
        return Failure<TValue>(error);
    }
}
