using Common.Application;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Database;

/// <summary>
///     Base class for the <c>DbContext</c> of a module. Each module owns its tables and its context;
///     the context is at the same time the unit of work of that module's use cases.
///     <para>
///         Entity configurations are picked up from the module's own assembly, so adding a mapping
///         never requires touching a central registration.
///     </para>
/// </summary>
public abstract class ModuleDbContext<TContext>(DbContextOptions<TContext> options)
    : DbContext(options), IUnitOfWork
    where TContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
    }
}
