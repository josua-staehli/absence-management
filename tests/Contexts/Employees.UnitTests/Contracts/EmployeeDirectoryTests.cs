using Employees.Application;
using Employees.UnitTests.UseCases;

namespace Employees.UnitTests.Contracts;

/// <summary>
///     The outward-facing side of the Employees bounded context.
/// </summary>
public class EmployeeDirectoryTests
{
    [Fact]
    public async Task An_employee_is_found_by_its_id()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var id = await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");

        var summary = await fixture.EmployeeDirectory.FindAsync(id);

        Assert.NotNull(summary);
        Assert.Equal(id, summary.Id);
        Assert.Equal("Anna Meier", summary.FullName);
        Assert.Equal("anna.meier@example.com", summary.Email);
    }

    /// <summary>
    ///     An unknown id is an answer, not a failure. The absences bounded context asks this
    ///     question to find out whether a request may be created at all.
    /// </summary>
    [Fact]
    public async Task An_unknown_id_is_answered_with_nothing()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");

        var summary = await fixture.EmployeeDirectory.FindAsync(Guid.CreateVersion7());

        Assert.Null(summary);
    }

    [Fact]
    public async Task Names_are_returned_for_a_whole_set_of_ids_at_once()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var anna = await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");
        var beat = await CreateAsync(fixture, "Beat", "Huber", "beat.huber@example.com");

        var names = await fixture.EmployeeDirectory.GetNamesAsync([anna, beat]);

        Assert.Equal(2, names.Count);
        Assert.Equal("Anna Meier", names[anna]);
        Assert.Equal("Beat Huber", names[beat]);
    }

    /// <summary>
    ///     Only what was asked for. An employee the caller did not name must not appear in the
    ///     answer, or a list would show rows it never asked about.
    /// </summary>
    [Fact]
    public async Task Only_the_names_that_were_asked_for_are_returned()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var anna = await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");
        await CreateAsync(fixture, "Beat", "Huber", "beat.huber@example.com");

        var names = await fixture.EmployeeDirectory.GetNamesAsync([anna]);

        Assert.Equal(anna, Assert.Single(names).Key);
    }

    /// <summary>
    ///     No foreign key spans the two databases, so an id without an employee is a state the
    ///     absences bounded context has to survive. It is left out of the answer rather than
    ///     reported, and the caller substitutes its own placeholder - see
    ///     <c>GetAbsenceRequestsHandler</c>, which calls <c>GetValueOrDefault</c> for exactly this.
    /// </summary>
    [Fact]
    public async Task An_id_without_an_employee_is_left_out_instead_of_failing()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var anna = await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");
        var unknown = Guid.CreateVersion7();

        var names = await fixture.EmployeeDirectory.GetNamesAsync([anna, unknown]);

        Assert.Equal("Anna Meier", names[anna]);
        Assert.False(names.ContainsKey(unknown));
    }

    /// <summary>
    ///     Nothing asked for, nothing to answer. The short circuit keeps a list of no rows from
    ///     reaching the database at all, and it has to hold the shape of the normal answer.
    /// </summary>
    [Fact]
    public async Task Asking_for_no_ids_returns_no_names()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");

        var names = await fixture.EmployeeDirectory.GetNamesAsync([]);

        Assert.Empty(names);
    }

    /// <summary>
    ///     Both methods compose the display name, and a caller may use them interchangeably: the
    ///     single request it looks up and the row in its list have to carry the same name.
    /// </summary>
    [Fact]
    public async Task Both_ways_of_asking_compose_the_same_display_name()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var id = await CreateAsync(fixture, "  Anna ", " Meier  ", "anna.meier@example.com");

        var summary = await fixture.EmployeeDirectory.FindAsync(id);
        var names = await fixture.EmployeeDirectory.GetNamesAsync([id]);

        Assert.NotNull(summary);
        Assert.Equal(summary.FullName, names[id]);
    }

    private static async Task<Guid> CreateAsync(
        EmployeesFixture fixture,
        string firstName,
        string lastName,
        string email)
    {
        var result = await new CreateEmployeeHandler(fixture.Employees, fixture.DbContext)
            .HandleAsync(new CreateEmployeeCommand(firstName, lastName, email));

        return result.Value;
    }
}
