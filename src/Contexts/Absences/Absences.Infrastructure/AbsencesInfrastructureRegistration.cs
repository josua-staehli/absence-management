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
    ///     Everything the absences bounded context needs to talk to the database. The concrete
    ///     types stay internal, only this one method is visible to the rest of the solution.
    ///     <para>
    ///         <c>IEmployeeDirectory</c> is deliberately absent: the employees bounded context
    ///         registers its own implementation, and this one only ever asks for the interface.
    ///     </para>
    /// </summary>
    public static IServiceCollection AddAbsencesInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddBoundedContextDbContext<AbsencesDbContext>(connectionString);

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
