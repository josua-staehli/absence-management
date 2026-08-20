using Absences.Domain;

namespace Absences.Application;

/// <summary>
///     The part of the read model this module owns. Everything in it comes out of the absences
///     database in a single query - which is why the employee appears as an id and not as a name.
/// </summary>
public sealed record AbsenceRequestRow(
    Guid Id,
    Guid EmployeeId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    AbsenceStatus Status,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
///     Read model of an absence request as the UI shows it: an <see cref="AbsenceRequestRow" />
///     plus the employee name, which only the employees module can supply. The two are joined in
///     the application layer and not in the database, because they live in two databases.
/// </summary>
public sealed record AbsenceRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    AbsenceStatus Status,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>
    ///     Shown when an id has no employee behind it anymore. Without a foreign key across the
    ///     two databases that is a state the module has to survive rather than one it can rule out,
    ///     so the list stays readable instead of failing.
    /// </summary>
    private const string UnknownEmployeeName = "Unknown employee";

    internal static AbsenceRequestDto From(AbsenceRequestRow row, string? employeeName)
    {
        return new AbsenceRequestDto(
            row.Id,
            row.EmployeeId,
            employeeName ?? UnknownEmployeeName,
            row.Type,
            row.StartDate,
            row.EndDate,
            row.Status,
            row.Comment,
            row.CreatedAt,
            row.UpdatedAt);
    }
}
