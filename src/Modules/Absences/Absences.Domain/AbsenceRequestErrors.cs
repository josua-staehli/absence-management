using Common.Domain.Results;

namespace Absences.Domain;

/// <summary>
///     The business errors of the <see cref="AbsenceRequest" /> aggregate. Declared in one place so
///     that the codes stay stable and can be referenced by the outer layers and the tests.
/// </summary>
public static class AbsenceRequestErrors
{
    public static readonly Error EmployeeRequired = Error.Validation(
        "Absences.EmployeeRequired",
        "An absence request requires an employee.");

    public static readonly Error EmployeeUnknown = Error.Validation(
        "Absences.EmployeeUnknown",
        "No employee with this id exists.");

    public static readonly Error TypeRequired = Error.Validation(
        "Absences.TypeRequired",
        "An absence request requires a valid absence type.");

    public static readonly Error EndDateBeforeStartDate = Error.Validation(
        "Absences.EndDateBeforeStartDate",
        "The start date must not be after the end date.");

    public static readonly Error CommentTooLong = Error.Validation(
        "Absences.CommentTooLong",
        $"The comment must not be longer than {AbsenceRequest.MaxCommentLength} characters.");

    public static readonly Error Overlapping = Error.Conflict(
        "Absences.Overlapping",
        "This employee already has an absence in this period.");

    public static readonly Error NotOpen = Error.Conflict(
        "Absences.NotOpen",
        "Only an open request can be edited, approved or rejected.");

    public static Error NotFound(Guid id)
    {
        return Error.NotFound("Absences.NotFound",
            $"No absence request with the id '{id}' exists.");
    }
}
