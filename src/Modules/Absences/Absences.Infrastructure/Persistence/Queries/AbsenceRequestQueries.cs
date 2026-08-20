using System.Linq.Expressions;
using Absences.Application;
using Absences.Domain;
using Microsoft.EntityFrameworkCore;

namespace Absences.Infrastructure.Persistence.Queries;

/// <summary>
///     Read side: projects into the row inside the database query, so no aggregate has to be
///     loaded or tracked just to render a table. The employee name is not part of it, it belongs
///     to another module and is added by the handler.
/// </summary>
internal sealed class AbsenceRequestQueries(AbsencesDbContext dbContext) : IAbsenceRequestQueries
{
    /// <summary>
    ///     Declared once so that both queries return the same shape. EF Core translates the
    ///     expression into the column list of the <c>SELECT</c>.
    /// </summary>
    private static readonly Expression<Func<AbsenceRequest, AbsenceRequestRow>> ToRow =
        absenceRequest => new AbsenceRequestRow(
            absenceRequest.Id,
            absenceRequest.EmployeeId,
            absenceRequest.Type,
            absenceRequest.Period.Start,
            absenceRequest.Period.End,
            absenceRequest.Status,
            absenceRequest.Comment,
            absenceRequest.CreatedAt,
            absenceRequest.UpdatedAt);

    public async Task<IReadOnlyList<AbsenceRequestRow>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AbsenceRequests
            .AsNoTracking()
            .OrderByDescending(absenceRequest => absenceRequest.Period.Start)
            .Select(ToRow)
            .ToListAsync(cancellationToken);
    }

    public Task<AbsenceRequestRow?> GetByIdAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AbsenceRequests
            .AsNoTracking()
            .Where(absenceRequest => absenceRequest.Id == id)
            .Select(ToRow)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
