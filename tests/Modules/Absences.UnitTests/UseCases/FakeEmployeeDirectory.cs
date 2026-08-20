using Employees.Contracts;

namespace Absences.UnitTests.UseCases;

/// <summary>
///     Stands in for the employees module. That the whole of it can be replaced by this class is
///     the point of the contract: these tests cannot break because something changes inside the
///     employees module, only because the two modules stop agreeing.
/// </summary>
internal sealed class FakeEmployeeDirectory : IEmployeeDirectory
{
    private readonly Dictionary<Guid, EmployeeSummary> _employees = [];

    public Task<EmployeeSummary?> FindAsync(Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_employees.GetValueOrDefault(employeeId));
    }

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, string> names = _employees
            .Where(employee => employeeIds.Contains(employee.Key))
            .ToDictionary(employee => employee.Key, employee => employee.Value.FullName);

        return Task.FromResult(names);
    }

    public FakeEmployeeDirectory With(Guid id, string fullName)
    {
        _employees[id] = new EmployeeSummary(id, fullName,
            $"{fullName.Replace(' ', '.').ToLowerInvariant()}@example.com");

        return this;
    }
}
