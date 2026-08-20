using Common.Infrastructure.Database;
using Employees.Application;
using Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence;

/// <summary>
///     The tables owned by the employees module, its own database, not a schema inside another
///     module's. The entity configurations are picked up automatically from this assembly by
///     <see cref="ModuleDbContext{TContext}" />.
/// </summary>
public sealed class EmployeesDbContext(DbContextOptions<EmployeesDbContext> options)
    : ModuleDbContext<EmployeesDbContext>(options), IEmployeesUnitOfWork
{
    public DbSet<Employee> Employees => Set<Employee>();
}
