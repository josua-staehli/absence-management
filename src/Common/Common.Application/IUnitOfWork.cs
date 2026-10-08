namespace Common.Application;

/// <summary>
///     Transaction boundary of a use case. Implemented by the bounded context's EF Core
///     <c>DbContext</c> in the infrastructure layer, so the application layer does not have to
///     know about EF Core.
///     <para>
///         A save that failed is not repeated on the same unit of work. If a domain event handler
///         failed, the unit of work refuses to save again: its events were taken out and only
///         partly handled, which cannot be replayed. Retry the whole use case in a fresh scope.
///     </para>
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
