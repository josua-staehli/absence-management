using Absences.Domain;

namespace Absences.Application;

/// <summary>
///     Write side access to the <see cref="AbsenceRequest" /> aggregate. Reads for the UI do not go
///     through this interface, they use the projections of <see cref="IAbsenceRequestQueries" />.
///     <para>
///         Deliberately not a generic <c>IRepository&lt;T&gt;</c>: every method here exists because
///         a use case calls it, so the interface says what the bounded context actually does with
///         the database.
///     </para>
/// </summary>
public interface IAbsenceRequestRepository
{
    /// <summary>
    ///     The tracked aggregate, so that a change to it is written by the unit of work.
    /// </summary>
    Task<AbsenceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     The absences of one employee must not overlap. That rule spans all requests of that
    ///     employee, so it cannot live inside a single aggregate and is answered by a query
    ///     instead.
    /// </summary>
    /// <param name="excludedRequestId">
    ///     The request that is being edited: it must not collide with itself.
    /// </param>
    Task<bool> HasOverlapAsync(
        Guid employeeId,
        DateRange period,
        Guid? excludedRequestId = null,
        CancellationToken cancellationToken = default);

    void Add(AbsenceRequest absenceRequest);
}
