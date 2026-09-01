using Common.Application;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Database;

/// <summary>
///     Base class for the <c>DbContext</c> of a bounded context. Each one owns its tables and its
///     <c>DbContext</c>, which is at the same time the unit of work of its use cases.
///     <para>
///         Entity configurations are picked up from the bounded context's own assembly, so adding
///         a mapping never requires touching a central registration.
///     </para>
/// </summary>
public abstract class BoundedContextDbContext<TContext>(DbContextOptions<TContext> options)
    : DbContext(options), IUnitOfWork
    where TContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);
    }
}
