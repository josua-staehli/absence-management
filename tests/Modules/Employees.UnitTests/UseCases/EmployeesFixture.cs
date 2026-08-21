using Employees.Infrastructure;
using Employees.Infrastructure.Persistence;
using Employees.Infrastructure.Persistence.Queries;
using Employees.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Employees.UnitTests.UseCases;

/// <summary>
///     Runs the use cases against an in memory SQLite database with the real EF Core mapping and
///     the real repository and queries. That covers the application layer together with the
///     persistence layer, and still needs no Docker and no PostgreSQL.
/// </summary>
internal sealed class EmployeesFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private EmployeesFixture(SqliteConnection connection, EmployeesDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
        Employees = new EmployeeRepository(dbContext);
        Queries = new EmployeeQueries(dbContext);
        EmployeeDirectory = new EmployeeDirectory(dbContext);
    }

    /// <summary>Also the handlers' unit of work, and the context the repository writes to.</summary>
    public EmployeesDbContext DbContext { get; }

    public EmployeeRepository Employees { get; }

    public EmployeeQueries Queries { get; }

    /// <summary>
    ///     The real implementation of the contract the other modules reach this one through, not
    ///     the fake that stands in for it on their side of the boundary.
    /// </summary>
    public EmployeeDirectory EmployeeDirectory { get; }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public static async Task<EmployeesFixture> CreateAsync()
    {
        // The database lives as long as the connection does, so the fixture holds it open.
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<EmployeesDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new EmployeesDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new EmployeesFixture(connection, dbContext);
    }
}
