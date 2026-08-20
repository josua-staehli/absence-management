using Absences.Application;
using Absences.Infrastructure.Persistence;
using Absences.Infrastructure.Persistence.Queries;
using Absences.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Absences.UnitTests.UseCases;

/// <summary>
///     Runs the use cases against an in memory SQLite database with the real EF Core mapping and
///     the real repository and queries. That covers the application layer together with the
///     persistence layer, and still needs no Docker and no PostgreSQL.
///     <para>
///         The employees module is not part of it: it is replaced by
///         <see cref="FakeEmployeeDirectory" />, so the only thing these tests share with it is the
///         contract.
///     </para>
/// </summary>
internal sealed class AbsencesFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private AbsencesFixture(SqliteConnection connection, AbsencesDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
        AbsenceRequests = new AbsenceRequestRepository(dbContext);
        Queries = new AbsenceRequestQueries(dbContext);
    }

    /// <summary>Also the handlers' unit of work, and the context the repository writes to.</summary>
    public AbsencesDbContext DbContext { get; }

    public AbsenceRequestRepository AbsenceRequests { get; }

    public AbsenceRequestQueries Queries { get; }

    public FakeEmployeeDirectory Employees { get; } = new();

    public TimeProvider Clock { get; } = TimeProvider.System;

    public Guid EmployeeId { get; } = new("11111111-1111-1111-1111-111111111111");

    public Guid OtherEmployeeId { get; } = new("22222222-2222-2222-2222-222222222222");

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public static async Task<AbsencesFixture> CreateAsync()
    {
        // The database lives as long as the connection does, so the fixture holds it open.
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AbsencesDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AbsencesDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var fixture = new AbsencesFixture(connection, dbContext);

        fixture.Employees
            .With(fixture.EmployeeId, "Anna Meier")
            .With(fixture.OtherEmployeeId, "Beat Huber");

        return fixture;
    }

    /// <summary>
    ///     The list as the API returns it: the rows of this module plus the names from the
    ///     employees module, assembled by the handler.
    /// </summary>
    public async Task<IReadOnlyList<AbsenceRequestDto>> ListAsync()
    {
        var handler = new GetAbsenceRequestsHandler(Queries, Employees);
        var result = await handler.HandleAsync(new GetAbsenceRequestsQuery());

        return result.Value;
    }
}
