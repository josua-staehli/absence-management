using Common.Domain.Results;

namespace Common.Application.Handlers;

/// <summary>
///     A use case that changes state. One handler per use case keeps the application layer
///     readable and testable. Callers depend on the handler interface, not on the implementation.
/// </summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>A use case that changes state and returns a value (e.g. the id of a new entity).</summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command,
        CancellationToken cancellationToken = default);
}
