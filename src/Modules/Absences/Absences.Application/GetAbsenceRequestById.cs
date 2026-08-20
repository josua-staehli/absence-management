using Absences.Domain;
using Common.Application.Handlers;
using Common.Domain.Results;
using Employees.Contracts;

namespace Absences.Application;

public sealed record GetAbsenceRequestByIdQuery(Guid Id);

internal sealed class GetAbsenceRequestByIdHandler(
    IAbsenceRequestQueries queries,
    IEmployeeDirectory employees) : IQueryHandler<GetAbsenceRequestByIdQuery, AbsenceRequestDto>
{
    public async Task<Result<AbsenceRequestDto>> HandleAsync(
        GetAbsenceRequestByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var row = await queries.GetByIdAsync(query.Id, cancellationToken);
        if (row is null) return AbsenceRequestErrors.NotFound(query.Id);

        // Same shape as the list, one employee instead of a set.
        var employee = await employees.FindAsync(row.EmployeeId, cancellationToken);

        return Result.Success(AbsenceRequestDto.From(row, employee?.FullName));
    }
}
