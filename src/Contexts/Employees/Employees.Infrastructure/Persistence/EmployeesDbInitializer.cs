using Common.Infrastructure.Database;
using Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employees.Infrastructure.Persistence;

/// <summary>
///     Applies the migrations of this bounded context and seeds the employees. Registered as an
///     <see cref="IDbInitializer" />, so the host runs it together with every other one's
///     initializer.
/// </summary>
internal sealed class EmployeesDbInitializer(EmployeesDbContext dbContext) : IDbInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Employees.AnyAsync(cancellationToken)) return;

        dbContext.Employees.AddRange(
            Seed("Anna", "Meier", "anna.meier@example.com"),
            Seed("Beat", "Huber", "beat.huber@example.com"),
            Seed("Clara", "Steiner", "clara.steiner@example.com"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     The seed goes through the same factory as every other employee, so invalid sample data
    ///     fails loudly on startup instead of sitting in the database as a row no use case could
    ///     have produced.
    /// </summary>
    private static Employee Seed(string firstName, string lastName, string email)
    {
        var employee = Employee.Create(firstName, lastName, email);

        return employee.IsSuccess
            ? employee.Value
            : throw new InvalidOperationException(
                $"Seed data for '{email}' is invalid: {employee.Error.Message}");
    }
}
