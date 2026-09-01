using Employees.Application;
using Employees.Domain;

namespace Employees.UnitTests.UseCases;

/// <summary>
///     The part of the creation that a pure domain test cannot cover: the uniqueness of the email
///     address, which is a rule about all employees, and the path through the repository into the
///     database.
/// </summary>
public class CreateEmployeeTests
{
    [Fact]
    public async Task Creating_stores_the_employee_and_makes_it_visible_in_the_list()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        var result = await Handler(fixture).HandleAsync(
            new CreateEmployeeCommand("Anna", "Meier", "anna.meier@example.com"));

        Assert.True(result.IsSuccess);

        var stored = Assert.Single(await fixture.Queries.ListAsync());

        Assert.Equal(result.Value, stored.Id);
        Assert.Equal("Anna", stored.FirstName);
        Assert.Equal("Meier", stored.LastName);
        Assert.Equal("anna.meier@example.com", stored.Email);
    }

    [Fact]
    public async Task An_address_that_is_already_in_use_is_rejected()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var handler = Handler(fixture);

        await handler.HandleAsync(
            new CreateEmployeeCommand("Anna", "Meier", "anna.meier@example.com"));

        var second = await handler.HandleAsync(
            new CreateEmployeeCommand("Andrea", "Meier", "anna.meier@example.com"));

        Assert.True(second.IsFailure);
        Assert.Equal(EmployeeErrors.EmailAlreadyInUse, second.Error);
        Assert.Single(await fixture.Queries.ListAsync());
    }

    /// <summary>
    ///     The use case compares the address of the aggregate, which is already trimmed. Comparing
    ///     the raw command value instead would let padding slip past the check and the unique index
    ///     would then fail with an exception rather than a business error.
    /// </summary>
    [Fact]
    public async Task An_address_that_only_differs_in_padding_is_rejected()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var handler = Handler(fixture);

        await handler.HandleAsync(
            new CreateEmployeeCommand("Anna", "Meier", "anna.meier@example.com"));

        var second = await handler.HandleAsync(
            new CreateEmployeeCommand("Andrea", "Meier", "  anna.meier@example.com  "));

        Assert.True(second.IsFailure);
        Assert.Equal(EmployeeErrors.EmailAlreadyInUse, second.Error);
    }

    /// <summary>
    ///     Uniqueness is case-sensitive today - the aggregate does not lower case the address, and
    ///     the unique index therefore does not either. This test pins that down, so that adding
    ///     the normalization later has to be a deliberate change.
    /// </summary>
    [Fact]
    public async Task Two_addresses_that_only_differ_in_casing_are_two_addresses()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();
        var handler = Handler(fixture);

        await handler.HandleAsync(
            new CreateEmployeeCommand("Anna", "Meier", "anna.meier@example.com"));

        var second = await handler.HandleAsync(
            new CreateEmployeeCommand("Andrea", "Meier", "Anna.Meier@Example.com"));

        Assert.True(second.IsSuccess);
        Assert.Equal(2, (await fixture.Queries.ListAsync()).Count);
    }

    [Fact]
    public async Task A_rejected_employee_is_not_stored()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        var result = await Handler(fixture).HandleAsync(
            new CreateEmployeeCommand("Anna", "Meier", "not-an-address"));

        Assert.True(result.IsFailure);
        Assert.Equal(EmployeeErrors.EmailInvalid, result.Error);
        Assert.Empty(await fixture.Queries.ListAsync());
    }

    [Fact]
    public async Task The_stored_employee_carries_the_trimmed_values()
    {
        await using var fixture = await EmployeesFixture.CreateAsync();

        var result = await Handler(fixture).HandleAsync(
            new CreateEmployeeCommand("  Anna ", " Meier  ", " anna.meier@example.com "));

        var stored = await fixture.Queries.GetByIdAsync(result.Value);

        Assert.NotNull(stored);
        Assert.Equal("Anna", stored.FirstName);
        Assert.Equal("Meier", stored.LastName);
        Assert.Equal("anna.meier@example.com", stored.Email);
    }

    private static CreateEmployeeHandler Handler(EmployeesFixture fixture)
    {
        return new CreateEmployeeHandler(fixture.Employees, fixture.DbContext);
    }
}
