using Employees.Contracts;
using Employees.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure;

/// <summary>
///     The outward-facing side of this bounded context: the only code another one reaches. It
///     answers with an <see cref="EmployeeSummary" /> - never with the aggregate - so the employees
///     bounded context can change its model without breaking anyone.
/// </summary>
internal sealed class EmployeeDirectory(EmployeesDbContext dbContext) : IEmployeeDirectory
{
    public Task<EmployeeSummary?> FindAsync(Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new EmployeeSummary(
                employee.Id,
                employee.FirstName + " " + employee.LastName,
                employee.Email))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        if (employeeIds.Count == 0) return new Dictionary<Guid, string>();

        return await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id))
            .ToDictionaryAsync(
                employee => employee.Id,
                employee => employee.FirstName + " " + employee.LastName,
                cancellationToken);
    }
}
