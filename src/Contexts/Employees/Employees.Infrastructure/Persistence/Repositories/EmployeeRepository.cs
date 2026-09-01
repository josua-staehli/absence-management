using Employees.Application;
using Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence.Repositories;

/// <summary>
///     Write side: a new aggregate is added to the same tracked context, so that
///     <see cref="IEmployeesUnitOfWork" /> writes every change of one use case in a single
///     transaction. Creating is the only state change an employee has, which is why there is no
///     load method here - the read side of <see cref="IEmployeeQueries" /> covers the rest.
/// </summary>
internal sealed class EmployeeRepository(EmployeesDbContext dbContext) : IEmployeeRepository
{
    public Task<bool> IsEmailInUseAsync(string email,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees.AnyAsync(employee => employee.Email == email,
            cancellationToken);
    }

    public void Add(Employee employee)
    {
        dbContext.Employees.Add(employee);
    }
}
