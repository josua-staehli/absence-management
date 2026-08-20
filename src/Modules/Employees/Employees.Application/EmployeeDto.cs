namespace Employees.Application;

/// <summary>Read model of an employee as it is shown in the UI.</summary>
public sealed record EmployeeDto(Guid Id, string FirstName, string LastName, string Email);
