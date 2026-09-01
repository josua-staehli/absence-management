using Common.Application.Handlers;
using Common.Domain.Results;
using Employees.Domain;

namespace Employees.Application;

public sealed record GetEmployeeByIdQuery(Guid Id);

internal sealed class GetEmployeeByIdHandler(IEmployeeQueries queries)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<Result<EmployeeDto>> HandleAsync(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var employee = await queries.GetByIdAsync(query.Id, cancellationToken);

        return employee is null
            ? EmployeeErrors.NotFound(query.Id)
            : Result.Success(employee);
    }
}
