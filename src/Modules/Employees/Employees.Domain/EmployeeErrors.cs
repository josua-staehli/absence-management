using Common.Domain.Results;

namespace Employees.Domain;

/// <summary>
///     The business errors of the <see cref="Employee" /> aggregate. Declared in one place so that
///     the codes stay stable and can be referenced by the outer layers and the tests.
/// </summary>
public static class EmployeeErrors
{
    public static readonly Error FirstNameRequired = Error.Validation(
        "Employees.FirstNameRequired",
        "The first name is required.");

    public static readonly Error LastNameRequired = Error.Validation(
        "Employees.LastNameRequired",
        "The last name is required.");

    public static readonly Error EmailRequired = Error.Validation(
        "Employees.EmailRequired",
        "The email address is required.");

    public static readonly Error EmailInvalid = Error.Validation(
        "Employees.EmailInvalid",
        "The email address is not a valid address.");

    public static readonly Error EmailAlreadyInUse = Error.Conflict(
        "Employees.EmailAlreadyInUse",
        "Another employee already uses this email address.");

    public static Error NotFound(Guid id)
    {
        return Error.NotFound("Employees.NotFound", $"No employee with the id '{id}' exists.");
    }
}
