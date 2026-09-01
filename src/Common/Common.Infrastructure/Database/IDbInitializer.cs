namespace Common.Infrastructure.Database;

/// <summary>
///     Migrates and seeds the tables of one bounded context. Every one of them brings its own
///     implementation.
/// </summary>
public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
