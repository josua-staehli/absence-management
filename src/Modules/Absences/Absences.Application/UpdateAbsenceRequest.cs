using Absences.Domain;
using Common.Application.Handlers;
using Common.Domain.Results;

namespace Absences.Application;

public sealed record UpdateAbsenceRequestCommand(
    Guid Id,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comment);

/// <summary>
///     An open request may be edited. The employee is not part of the command, it is the one
///     value of a request that does not change, see <see cref="AbsenceRequest.Update" />.
/// </summary>
internal sealed class UpdateAbsenceRequestHandler(
    IAbsenceRequestRepository absenceRequests,
    IAbsencesUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<UpdateAbsenceRequestCommand>
{
    public async Task<Result> HandleAsync(
        UpdateAbsenceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var absenceRequest = await absenceRequests.GetByIdAsync(command.Id, cancellationToken);
        if (absenceRequest is null) return AbsenceRequestErrors.NotFound(command.Id);

        // That an approved or rejected request is no longer editable belongs to the aggregate, and
        // Update() enforces it again. Asking here as well is about the answer the caller gets: a
        // decided request should be reported as decided, and not as something that happens to
        // collide with another absence.
        if (!absenceRequest.IsOpen) return AbsenceRequestErrors.NotOpen;

        var period = DateRange.Create(command.StartDate, command.EndDate);
        if (period.IsFailure) return period.Error;

        // The overlap check again, this time ignoring the request itself: a request always
        // overlaps its own period, and moving it by a day would otherwise be impossible.
        if (await absenceRequests.HasOverlapAsync(absenceRequest.EmployeeId, period.Value,
                absenceRequest.Id, cancellationToken))
            return AbsenceRequestErrors.Overlapping;

        var result = absenceRequest.Update(command.Type, period.Value, command.Comment,
            timeProvider.GetUtcNow());
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
