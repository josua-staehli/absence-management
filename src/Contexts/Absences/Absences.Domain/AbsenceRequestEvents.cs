using Common.Domain.Primitives;

namespace Absences.Domain;

/// <summary>
///     A request was filed, see <see cref="AbsenceRequest.Create" />. It starts as open.
///     <para>
///         Every event of the <see cref="AbsenceRequest" /> aggregate names the absence it is about
///         the same way: whose it is, what kind, which days. That is what a reaction needs - a
///         notification, a calendar entry, a vacation balance - so none has to load the request.
///         The comment is left out, it is a note for the approver and nothing to react to.
///     </para>
/// </summary>
public sealed record AbsenceRequestCreated(
    Guid AbsenceRequestId,
    Guid EmployeeId,
    AbsenceType Type,
    DateRange Period) : IDomainEvent;

/// <summary>An open request was edited. Type and period are the new values.</summary>
public sealed record AbsenceRequestUpdated(
    Guid AbsenceRequestId,
    Guid EmployeeId,
    AbsenceType Type,
    DateRange Period) : IDomainEvent;

/// <summary>A request was approved. The decision is final, the absence will happen.</summary>
public sealed record AbsenceRequestApproved(
    Guid AbsenceRequestId,
    Guid EmployeeId,
    AbsenceType Type,
    DateRange Period) : IDomainEvent;

/// <summary>A request was rejected. The decision is final, and the period is free again.</summary>
public sealed record AbsenceRequestRejected(
    Guid AbsenceRequestId,
    Guid EmployeeId,
    AbsenceType Type,
    DateRange Period) : IDomainEvent;
