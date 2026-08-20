using Common.Infrastructure;
using Common.Infrastructure.Database;
using Employees.Application;
using Employees.Contracts;
using Employees.Infrastructure.Persistence;
using Employees.Infrastructure.Persistence.Queries;
using Employees.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Employees.Infrastructure;

public static class EmployeesInfrastructureRegistration
{
    /// <summary>
    ///     Everything the employees module needs to talk to the database. The concrete types stay
    ///     internal; only this one method is visible to the rest of the solution.
    /// </summary>
    public static IServiceCollection AddEmployeesInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddModuleDbContext<EmployeesDbContext>(connectionString);

        // The unit of work resolves to the very same context instance the repository writes to,
        // otherwise a use case would save through a second context that never saw its changes.
        services.AddScoped<IEmployeesUnitOfWork>(provider =>
            provider.GetRequiredService<EmployeesDbContext>());

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeQueries, EmployeeQueries>();

        // The published contract. Registering it here allows another module to depend on the
        // interface without knowing what implements it.
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();

        services.AddDbInitializer<EmployeesDbInitializer>();

        return services;
    }
}
