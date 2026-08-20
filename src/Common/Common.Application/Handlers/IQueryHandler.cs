using Common.Domain.Results;

namespace Common.Application.Handlers;

/// <summary>A use case that only reads data.</summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>>
        HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
