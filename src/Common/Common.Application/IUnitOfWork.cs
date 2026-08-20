namespace Common.Application;

/// <summary>
///     Transaction boundary of a use case. Implemented by the module's EF Core <c>DbContext</c> in the
///     infrastructure layer, so the application layer does not have to know about EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
