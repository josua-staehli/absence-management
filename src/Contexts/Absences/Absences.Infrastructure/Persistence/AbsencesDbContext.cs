using Absences.Application;
using Absences.Domain;
using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Absences.Infrastructure.Persistence;

/// <summary>
///     The tables owned by the absences bounded context, its own database, not a schema inside
///     another one's. The entity configurations are picked up automatically from this assembly by
///     <see cref="BoundedContextDbContext{TContext}" />.
///     <para>
///         Employees are not among them: they are a bounded context of their own, with a database
///         of their own. This <c>DbContext</c> cannot see them, which is what makes the boundary
///         real rather than a convention.
///     </para>
/// </summary>
public sealed class AbsencesDbContext(DbContextOptions<AbsencesDbContext> options)
    : BoundedContextDbContext<AbsencesDbContext>(options), IAbsencesUnitOfWork
{
    public DbSet<AbsenceRequest> AbsenceRequests => Set<AbsenceRequest>();
}
