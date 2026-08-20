namespace Absences.Application;

/// <summary>
///     Read side of the absence requests. Separate from <see cref="IAbsenceRequestRepository" />: a
///     list for the UI needs no aggregate, no tracking and no invariants, only a projection.
/// </summary>
public interface IAbsenceRequestQueries
{
    Task<IReadOnlyList<AbsenceRequestRow>> ListAsync(CancellationToken cancellationToken = default);

    Task<AbsenceRequestRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
