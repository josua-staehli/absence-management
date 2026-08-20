using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence;

/// <summary>
///     Used by <c>dotnet ef</c> only, see <see cref="DesignTimeDbContextFactory{TContext}" />.
/// </summary>
internal sealed class DesignTimeDbContextFactory()
    : DesignTimeDbContextFactory<EmployeesDbContext>("employeedb")
{
    protected override EmployeesDbContext Create(DbContextOptions<EmployeesDbContext> options)
    {
        return new EmployeesDbContext(options);
    }
}
