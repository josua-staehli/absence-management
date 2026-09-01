namespace Employees.Application;

/// <summary>
///     Read side of the employees. Separate from <see cref="IEmployeeRepository" />: a list for the
///     UI needs no aggregate, no tracking and no invariants, only a projection.
/// </summary>
public interface IEmployeeQueries
{
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
