using Employees.Domain;

namespace Employees.Application;

/// <summary>
///     Write side access to the <see cref="Employee" /> aggregate. Reads for the UI do not go
///     through this interface, they use the projections of <see cref="IEmployeeQueries" />.
///     <para>
///         Deliberately not a generic <c>IRepository&lt;T&gt;</c>: every method here exists
///         because a use case calls it, so the interface says what the bounded context actually
///         does with the database.
///     </para>
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    ///     Uniqueness spans the whole table, so it cannot be a rule of a single aggregate. The
    ///     database enforces it with a unique index; this method exists so the use case can report a
    ///     business error instead of letting a constraint violation escape as an exception.
    /// </summary>
    Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken = default);

    void Add(Employee employee);
}
