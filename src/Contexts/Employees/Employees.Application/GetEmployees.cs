using Common.Application.Handlers;
using Common.Domain.Results;

namespace Employees.Application;

/// <summary>All employees, e.g. for a list or a selection in the UI.</summary>
public sealed record GetEmployeesQuery;

internal sealed class GetEmployeesHandler(IEmployeeQueries queries)
    : IQueryHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public async Task<Result<IReadOnlyList<EmployeeDto>>> HandleAsync(
        GetEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        return Result.Success(await queries.ListAsync(cancellationToken));
    }
}
