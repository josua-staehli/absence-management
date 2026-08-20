using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Absences.Infrastructure.Persistence;

/// <summary>
///     Used by <c>dotnet ef</c> only, see <see cref="DesignTimeDbContextFactory{TContext}" />.
/// </summary>
internal sealed class DesignTimeDbContextFactory()
    : DesignTimeDbContextFactory<AbsencesDbContext>("absencedb")
{
    protected override AbsencesDbContext Create(DbContextOptions<AbsencesDbContext> options)
    {
        return new AbsencesDbContext(options);
    }
}
