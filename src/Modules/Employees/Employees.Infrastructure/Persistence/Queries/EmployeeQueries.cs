using System.Linq.Expressions;
using Employees.Application;
using Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence.Queries;

/// <summary>
///     Read side: projects into the DTO inside the database query, so no aggregate has to be
///     loaded or tracked just to render a table.
/// </summary>
internal sealed class EmployeeQueries(EmployeesDbContext dbContext) : IEmployeeQueries
{
    /// <summary>
    ///     Declared once so that both queries return the same shape. EF Core translates the
    ///     expression into the column list of the <c>SELECT</c>.
    /// </summary>
    private static readonly Expression<Func<Employee, EmployeeDto>> ToDto =
        employee => new EmployeeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email);

    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
