using Common.Application.Handlers;
using Common.Domain.Results;
using Employees.Domain;

namespace Employees.Application;

public sealed record CreateEmployeeCommand(string FirstName, string LastName, string Email);

internal sealed class CreateEmployeeHandler(
    IEmployeeRepository employees,
    IEmployeesUnitOfWork unitOfWork) : ICommandHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var employee = Employee.Create(command.FirstName, command.LastName, command.Email);
        if (employee.IsFailure) return employee.Error;

        // Uniqueness is a rule about the whole set of employees, not about a single one, so it is
        // checked here and not inside the aggregate. The address of the aggregate is used, because
        // only that one is trimmed the same way as the addresses already stored.
        if (await employees.IsEmailInUseAsync(employee.Value.Email, cancellationToken))
            return EmployeeErrors.EmailAlreadyInUse;

        employees.Add(employee.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Value.Id;
    }
}
