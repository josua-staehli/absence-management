using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Absences.Infrastructure.Persistence;

/// <summary>
///     Applies the migrations of this module. Registered as an <see cref="IDbInitializer" />, so
///     the host runs it together with every other module's initializer.
///     <para>
///         Nothing is seeded here: an absence request needs an employee, and the ids of the seeded
///         employees belong to the module that owns them.
///     </para>
/// </summary>
internal sealed class AbsencesDbInitializer(AbsencesDbContext dbContext) : IDbInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}
