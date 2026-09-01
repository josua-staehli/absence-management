using Absences.Domain;

namespace Absences.Api.Contracts;

/// <summary>
///     Request model of the HTTP API. It is kept separate from the application command, so the
///     public contract can evolve independently of the internal use case.
/// </summary>
public sealed record CreateAbsenceRequestRequest(
    Guid EmployeeId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comment);

/// <summary>
///     The editable part of an absence request. The employee is missing on purpose: a request for
///     somebody else is a new request, not an edited one.
/// </summary>
public sealed record UpdateAbsenceRequestRequest(
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comment);

/// <summary>
///     Body of the <c>201 Created</c> response. A named type, not an anonymous object, so it
///     appears in the OpenAPI document a frontend client is generated from.
/// </summary>
public sealed record CreateAbsenceRequestResponse(Guid Id);
