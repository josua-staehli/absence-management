namespace Common.Infrastructure.Database;

/// <summary>
///     Migrates and seeds the tables of one module. Every module brings its own implementation.
/// </summary>
public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
