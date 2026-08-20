namespace Employees.Contracts;

/// <summary>
///     What another module is allowed to know about an employee: an id, a display name and an
///     email address. Everything else - the aggregate, its rules, its table - stays inside the
///     employees module.
/// </summary>
public sealed record EmployeeSummary(Guid Id, string FullName, string Email);

/// <summary>
///     The one way into the employees module from outside. Another module references this project
///     and nothing else of <c>Employees.*</c>; the implementation lives in
///     <c>Employees.Infrastructure</c> and is registered by the employees module itself.
/// </summary>
public interface IEmployeeDirectory
{
    /// <summary>Returns <c>null</c> when no employee with this id exists.</summary>
    Task<EmployeeSummary?> FindAsync(Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Display names for a whole set of ids at once, so a list costs one call instead of one
    ///     per row. Ids without an employee are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default);
}
