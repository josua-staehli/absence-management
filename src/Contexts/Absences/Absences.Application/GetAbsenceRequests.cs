using Common.Application.Handlers;
using Common.Domain.Results;
using Employees.Contracts;

namespace Absences.Application;

/// <summary>All absence requests, as the overview in the UI shows them.</summary>
public sealed record GetAbsenceRequestsQuery;

/// <summary>
///     Two bounded contexts own two halves of this list, so it is one query per bounded context
///     and a lookup in memory instead of a SQL join. That is the price of the boundary.
/// </summary>
internal sealed class GetAbsenceRequestsHandler(
    IAbsenceRequestQueries queries,
    IEmployeeDirectory employees)
    : IQueryHandler<GetAbsenceRequestsQuery, IReadOnlyList<AbsenceRequestDto>>
{
    public async Task<Result<IReadOnlyList<AbsenceRequestDto>>> HandleAsync(
        GetAbsenceRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = await queries.ListAsync(cancellationToken);

        if (rows.Count == 0) return Result.Success<IReadOnlyList<AbsenceRequestDto>>([]);

        // One call for the whole list, not one per row: crossing a bounded context boundary inside
        // a loop is the easiest way to turn a list into an N+1.
        var employeeIds = rows.Select(row => row.EmployeeId).Distinct().ToArray();
        var names = await employees.GetNamesAsync(employeeIds, cancellationToken);

        var absenceRequests = rows
            .Select(row => AbsenceRequestDto.From(row, names.GetValueOrDefault(row.EmployeeId)))
            .ToList();

        return Result.Success<IReadOnlyList<AbsenceRequestDto>>(absenceRequests);
    }
}
