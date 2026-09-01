using Employees.Application;
using Employees.Domain;

namespace Employees.UnitTests.UseCases;

/// <summary>
///     The read side: what the UI gets to see. These run against the real projections, so the
///     ordering and the shape of the DTO are covered as well.
/// </summary>
public class EmployeeQueryTests
{
    [Fact]
    public async Task Without_employees_the_list_is_empty_and_still_a_success()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        var result = await new GetEmployeesHandler(fixture.Queries)
            .HandleAsync(new GetEmployeesQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task The_list_is_ordered_by_last_name_and_then_by_first_name()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        await CreateAsync(fixture, "Clara", "Steiner", "clara@example.com");
        await CreateAsync(fixture, "Beat", "Meier", "beat@example.com");
        await CreateAsync(fixture, "Anna", "Meier", "anna@example.com");

        var result = await new GetEmployeesHandler(fixture.Queries)
            .HandleAsync(new GetEmployeesQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(["Anna Meier", "Beat Meier", "Clara Steiner"],
            result.Value.Select(employee => $"{employee.FirstName} {employee.LastName}"));
    }

    [Fact]
    public async Task An_employee_can_be_looked_up_by_its_id()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        await CreateAsync(fixture, "Beat", "Huber", "beat.huber@example.com");
        var id = await CreateAsync(fixture, "Anna", "Meier", "anna.meier@example.com");

        var result = await new GetEmployeeByIdHandler(fixture.Queries)
            .HandleAsync(new GetEmployeeByIdQuery(id));

        Assert.True(result.IsSuccess);
        Assert.Equal(new EmployeeDto(id, "Anna", "Meier", "anna.meier@example.com"), result.Value);
    }

    [Fact]
    public async Task An_unknown_id_reports_not_found_instead_of_an_empty_employee()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var id = Guid.CreateVersion7();

        var result = await new GetEmployeeByIdHandler(fixture.Queries)
            .HandleAsync(new GetEmployeeByIdQuery(id));

        Assert.True(result.IsFailure);
        Assert.Equal(EmployeeErrors.NotFound(id), result.Error);
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
