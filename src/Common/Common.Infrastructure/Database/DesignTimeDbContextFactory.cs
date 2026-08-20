using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Common.Infrastructure.Database;

/// <summary>
///     Base for the design time factory of a module, used by <c>dotnet ef</c> only. The connection
///     string is never opened while creating a migration, so no running database is required to
///     add one - which is why a hard-coded local address is enough, and why the only thing a
///     module has to say is which database it owns.
/// </summary>
public abstract class DesignTimeDbContextFactory<TContext>(string databaseName)
    : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(
                $"Host=localhost;Database={databaseName};Username=postgres;Password=design-time")
            .Options;

        return Create(options);
    }

    /// <summary>
    ///     Calls the module's own <c>DbContext</c> constructor. A generic base cannot do that
    ///     without reflection, and one line per module is the cheaper of the two.
    /// </summary>
    protected abstract TContext Create(DbContextOptions<TContext> options);
}
