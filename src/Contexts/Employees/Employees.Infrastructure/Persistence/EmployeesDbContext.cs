using Common.Infrastructure.Database;
using Employees.Application;
using Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence;

/// <summary>
///     The tables owned by the employees bounded context, its own database, not a schema inside
///     another one's. The entity configurations are picked up automatically from this assembly by
///     <see cref="BoundedContextDbContext{TContext}" />.
/// </summary>
public sealed class EmployeesDbContext(DbContextOptions<EmployeesDbContext> options)
    : BoundedContextDbContext<EmployeesDbContext>(options), IEmployeesUnitOfWork
{
    public DbSet<Employee> Employees => Set<Employee>();
}
