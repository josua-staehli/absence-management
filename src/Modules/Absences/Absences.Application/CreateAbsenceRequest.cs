using Absences.Domain;
using Common.Application.Handlers;
using Common.Domain.Results;
using Employees.Contracts;

namespace Absences.Application;

public sealed record CreateAbsenceRequestCommand(
    Guid EmployeeId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comment);

internal sealed class CreateAbsenceRequestHandler(
    IAbsenceRequestRepository absenceRequests,
    IEmployeeDirectory employees,
    IAbsencesUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateAbsenceRequestCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateAbsenceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        // That the start is not after the end lives in the value object, so an invalid period
        // never reaches the aggregate.
        var period = DateRange.Create(command.StartDate, command.EndDate);
        if (period.IsFailure)
        {
            return period.Error;
        }

        // A request needs an employee, and the employee lives in another module: this is a
        // question asked across the boundary rather than a join or a foreign key. The answer
        // carries exactly what this module needs to know - the employee exists - and nothing else.
        if (await employees.FindAsync(command.EmployeeId, cancellationToken) is null)
        {
            return AbsenceRequestErrors.EmployeeUnknown;
        }

        // Absences of one employee must not overlap. That spans all of their requests, so it is
        // checked here and not in the aggregate, which only ever sees itself.
        if (await absenceRequests.HasOverlapAsync(command.EmployeeId, period.Value,
                cancellationToken: cancellationToken))
        {
            return AbsenceRequestErrors.Overlapping;
        }

        var absenceRequest = AbsenceRequest.Create(
            command.EmployeeId,
            command.Type,
            period.Value,
            command.Comment,
            timeProvider.GetUtcNow());

        if (absenceRequest.IsFailure)
        {
            return absenceRequest.Error;
        }

        absenceRequests.Add(absenceRequest.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return absenceRequest.Value.Id;
    }
}
