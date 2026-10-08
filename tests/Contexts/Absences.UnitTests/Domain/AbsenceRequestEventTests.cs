using Absences.Domain;

namespace Absences.UnitTests.Domain;

/// <summary>
///     What the aggregate reports about itself: every successful change raises exactly one event,
///     a refused one raises none. Like the rules, all of it is visible without a database.
/// </summary>
public class AbsenceRequestEventTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creating_a_request_raises_created()
    {
        var request = AbsenceRequest.Create(EmployeeId, AbsenceType.Vacation, Period(), null, Now)
            .Value;

        var created = Assert.Single(request.DomainEvents);
        Assert.Equal(
            new AbsenceRequestCreated(request.Id, EmployeeId, AbsenceType.Vacation, Period()),
            created);
    }

    [Fact]
    public void Editing_a_request_raises_updated_with_the_new_values()
    {
        var request = SavedRequest();
        var period = DateRange.Create(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3)).Value;

        request.Update(AbsenceType.Training, period, "Conference", Now);

        var updated = Assert.Single(request.DomainEvents);
        Assert.Equal(
            new AbsenceRequestUpdated(request.Id, EmployeeId, AbsenceType.Training, period),
            updated);
    }

    [Fact]
    public void Approving_a_request_raises_approved()
    {
        var request = SavedRequest();

        request.Approve(Now);

        var approved = Assert.Single(request.DomainEvents);
        Assert.Equal(
            new AbsenceRequestApproved(request.Id, EmployeeId, AbsenceType.Vacation, Period()),
            approved);
    }

    [Fact]
    public void Rejecting_a_request_raises_rejected()
    {
        var request = SavedRequest();

        request.Reject(Now);

        var rejected = Assert.Single(request.DomainEvents);
        Assert.Equal(
            new AbsenceRequestRejected(request.Id, EmployeeId, AbsenceType.Vacation, Period()),
            rejected);
    }

    /// <summary>A refused change is no change, so there is nothing to report.</summary>
    [Fact]
    public void A_refused_change_raises_nothing()
    {
        var request = SavedRequest();
        request.Approve(Now);
        request.ClearDomainEvents();

        request.Approve(Now);
        request.Reject(Now);
        request.Update(AbsenceType.Training, Period(), null, Now);

        Assert.Empty(request.DomainEvents);
    }

    private static DateRange Period()
    {
        return DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)).Value;
    }

    /// <summary>
    ///     A request as a use case loads it: created and saved, so its creation has been reported
    ///     already, the way the unit of work leaves it.
    /// </summary>
    private static AbsenceRequest SavedRequest()
    {
        var request = AbsenceRequest.Create(EmployeeId, AbsenceType.Vacation, Period(), null, Now)
            .Value;

        request.ClearDomainEvents();

        return request;
    }
}
