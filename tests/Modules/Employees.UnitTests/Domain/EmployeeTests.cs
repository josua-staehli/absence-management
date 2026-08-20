using Common.Domain.Results;
using Employees.Domain;

namespace Employees.UnitTests.Domain;

/// <summary>
///     The rules of the employee aggregate. Creation is the only state change it has, and none of
///     these rules needs a database.
/// </summary>
public class EmployeeTests
{
    [Fact]
    public void A_created_employee_keeps_the_values_it_was_created_with()
    {
        var employee = Create();

        Assert.True(employee.IsSuccess);
        Assert.Equal("Anna", employee.Value.FirstName);
        Assert.Equal("Meier", employee.Value.LastName);
        Assert.Equal("anna.meier@example.com", employee.Value.Email);
    }

    [Fact]
    public void Every_employee_gets_an_identity_of_its_own()
    {
        var anna = Create().Value;
        var beat = Create("Beat", "Huber", "beat.huber@example.com").Value;

        Assert.NotEqual(Guid.Empty, anna.Id);
        Assert.NotEqual(Guid.Empty, beat.Id);
        Assert.NotEqual(anna.Id, beat.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Creating_without_a_first_name_fails(string? firstName)
    {
        var employee = Create(firstName);

        Assert.True(employee.IsFailure);
        Assert.Equal(EmployeeErrors.FirstNameRequired, employee.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Creating_without_a_last_name_fails(string? lastName)
    {
        var employee = Create(lastName: lastName);

        Assert.True(employee.IsFailure);
        Assert.Equal(EmployeeErrors.LastNameRequired, employee.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Creating_without_an_email_address_fails(string? email)
    {
        var employee = Create(email: email);

        Assert.True(employee.IsFailure);
        Assert.Equal(EmployeeErrors.EmailRequired, employee.Error);
    }

    [Theory]
    [InlineData("not-an-address")]
    [InlineData("anna.meier@")]
    [InlineData("@example.com")]
    [InlineData("anna@meier@example.com")]
    public void Creating_with_an_address_of_the_wrong_shape_fails(string email)
    {
        var employee = Create(email: email);

        Assert.True(employee.IsFailure);
        Assert.Equal(EmployeeErrors.EmailInvalid, employee.Error);
    }

    [Fact]
    public void The_aggregate_never_holds_untrimmed_values()
    {
        var employee = Create("  Anna ", " Meier  ", "  anna.meier@example.com  ");

        Assert.True(employee.IsSuccess);
        Assert.Equal("Anna", employee.Value.FirstName);
        Assert.Equal("Meier", employee.Value.LastName);
        Assert.Equal("anna.meier@example.com", employee.Value.Email);
    }

    private static Result<Employee> Create(
        string? firstName = "Anna",
        string? lastName = "Meier",
        string? email = "anna.meier@example.com")
    {
        // The aggregate rejects null itself, so the tests may pass it despite the signature.
        return Employee.Create(firstName!, lastName!, email!);
    }
}
