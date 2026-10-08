using Common.Domain.Primitives;

namespace Employees.Domain;

/// <summary>
///     An employee was created, see <see cref="Employee.Create" />. The values are the trimmed ones
///     the aggregate holds.
/// </summary>
public sealed record EmployeeCreated(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string Email) : IDomainEvent;
