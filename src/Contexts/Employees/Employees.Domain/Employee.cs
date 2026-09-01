using Common.Domain.Primitives;
using Common.Domain.Results;

namespace Employees.Domain;

/// <summary>
///     Aggregate root of an employee. Every state change goes through a method on this class, so
///     the business rules cannot be bypassed by the API, the application layer or EF Core.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>
{
    private Employee(Guid id, string firstName, string lastName, string email) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    /// <summary>Used by EF Core for materialization.</summary>
    private Employee()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    /// <summary>
    ///     The only way to create an employee. Whitespace around the values is removed, so that
    ///     the aggregate never holds an untrimmed value.
    /// </summary>
    public static Result<Employee> Create(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName)) return EmployeeErrors.FirstNameRequired;

        if (string.IsNullOrWhiteSpace(lastName)) return EmployeeErrors.LastNameRequired;

        if (string.IsNullOrWhiteSpace(email)) return EmployeeErrors.EmailRequired;

        var trimmedEmail = email.Trim();
        if (!IsEmailAddress(trimmedEmail)) return EmployeeErrors.EmailInvalid;

        return new Employee(Guid.CreateVersion7(), firstName.Trim(), lastName.Trim(), trimmedEmail);
    }

    /// <summary>
    ///     A shape check only: exactly one <c>@</c>, with something on either side. Deliverability
    ///     is not something a regular expression can answer, so the check stays this small.
    /// </summary>
    private static bool IsEmailAddress(string email)
    {
        var parts = email.Split('@');

        return parts.Length == 2 && parts.All(part => part.Length > 0);
    }
}
