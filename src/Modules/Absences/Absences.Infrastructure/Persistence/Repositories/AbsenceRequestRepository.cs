using Absences.Application;
using Absences.Domain;
using Microsoft.EntityFrameworkCore;

namespace Absences.Infrastructure.Persistence.Repositories;

/// <summary>
///     Write side: the aggregate is loaded and stored as a whole and stays tracked, so that
///     <see cref="IAbsencesUnitOfWork" /> writes every change of one use case in a single
///     transaction.
/// </summary>
internal sealed class AbsenceRequestRepository(AbsencesDbContext dbContext)
    : IAbsenceRequestRepository
{
    public Task<AbsenceRequest?> GetByIdAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AbsenceRequests.SingleOrDefaultAsync(
            absenceRequest => absenceRequest.Id == id,
            cancellationToken);
    }

    public Task<bool> HasOverlapAsync(
        Guid employeeId,
        DateRange period,
        Guid? excludedRequestId = null,
        CancellationToken cancellationToken = default)
    {
        // Both periods include their end, so touching on a single day already counts as an
        // overlap. Pulled into locals because EF Core translates the value object's properties,
        // not a captured DateRange.
        var start = period.Start;
        var end = period.End;

        return dbContext.AbsenceRequests.AnyAsync(
            absenceRequest => absenceRequest.EmployeeId == employeeId
                              && absenceRequest.Id != excludedRequestId
                              // A rejected request is not an absence, so it blocks nothing.
                              && absenceRequest.Status != AbsenceStatus.Rejected
                              && absenceRequest.Period.Start <= end
                              && start <= absenceRequest.Period.End,
            cancellationToken);
    }

    public void Add(AbsenceRequest absenceRequest)
    {
        dbContext.AbsenceRequests.Add(absenceRequest);
    }
}
