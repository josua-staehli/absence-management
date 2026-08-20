using Absences.Domain;
using Common.Application.Handlers;
using Common.Domain.Results;

namespace Absences.Application;

public sealed record RejectAbsenceRequestCommand(Guid Id);

internal sealed class RejectAbsenceRequestHandler(
    IAbsenceRequestRepository absenceRequests,
    IAbsencesUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<RejectAbsenceRequestCommand>
{
    public async Task<Result> HandleAsync(
        RejectAbsenceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var absenceRequest = await absenceRequests.GetByIdAsync(command.Id, cancellationToken);
        if (absenceRequest is null) return AbsenceRequestErrors.NotFound(command.Id);

        // The decision itself is a rule of the aggregate, the handler only orchestrates.
        var result = absenceRequest.Reject(timeProvider.GetUtcNow());
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
