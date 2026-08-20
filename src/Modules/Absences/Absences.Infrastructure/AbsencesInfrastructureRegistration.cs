using Absences.Application;
using Absences.Infrastructure.Persistence;
using Absences.Infrastructure.Persistence.Queries;
using Absences.Infrastructure.Persistence.Repositories;
using Common.Infrastructure;
using Common.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Absences.Infrastructure;

public static class AbsencesInfrastructureRegistration
{
    /// <summary>
    ///     Everything the absences module needs to talk to the database. The concrete types stay
    ///     internal, only this one method is visible to the rest of the solution.
    ///     <para>
    ///         <c>IEmployeeDirectory</c> is deliberately absent: the employees module registers its
    ///         own implementation, and this module only ever asks for the interface.
    ///     </para>
    /// </summary>
    public static IServiceCollection AddAbsencesInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddModuleDbContext<AbsencesDbContext>(connectionString);

        // The unit of work resolves to the very same context instance the repository writes to,
        // otherwise a use case would save through a second context that never saw its changes.
        services.AddScoped<IAbsencesUnitOfWork>(provider =>
            provider.GetRequiredService<AbsencesDbContext>());

        services.AddScoped<IAbsenceRequestRepository, AbsenceRequestRepository>();
        services.AddScoped<IAbsenceRequestQueries, AbsenceRequestQueries>();

        services.AddDbInitializer<AbsencesDbInitializer>();

        return services;
    }
}
