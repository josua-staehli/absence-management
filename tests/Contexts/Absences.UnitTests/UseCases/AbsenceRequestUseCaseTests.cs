using Absences.Application;
using Absences.Domain;

namespace Absences.UnitTests.UseCases;

/// <summary>
///     What a domain test cannot reach: the rules that span more than one request, the question
///     asked across the bounded context boundary, whether the values survive the trip through the
///     database, and which domain events the saves dispatch.
/// </summary>
public class AbsenceRequestUseCaseTests
{
    [Fact]
    public async Task Creating_a_request_stores_it_and_makes_it_visible_in_the_list()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var result = await CreateHandler(fixture).HandleAsync(
            new CreateAbsenceRequestCommand(
                fixture.EmployeeId,
                AbsenceType.Vacation,
                new DateOnly(2026, 3, 2),
                new DateOnly(2026, 3, 6),
                "Ski holiday"));

        Assert.True(result.IsSuccess);

        var stored = Assert.Single(await fixture.ListAsync());

        Assert.Equal(result.Value, stored.Id);
        Assert.Equal("Anna Meier", stored.EmployeeName);
        Assert.Equal(AbsenceStatus.Open, stored.Status);
        Assert.Equal(new DateOnly(2026, 3, 2), stored.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 6), stored.EndDate);
        Assert.Equal("Ski holiday", stored.Comment);
    }

    /// <summary>
    ///     A request must not be created without an employee. The half of that rule which only
    ///     the employees bounded context can answer.
    /// </summary>
    [Fact]
    public async Task A_request_for_an_unknown_employee_is_rejected()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var result = await CreateHandler(fixture)
            .HandleAsync(Command(Guid.CreateVersion7(), "2026-03-02", "2026-03-06"));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.EmployeeUnknown, result.Error);
        Assert.Empty(await fixture.ListAsync());
    }

    /// <summary>Absences of the same employee must not overlap in time.</summary>
    [Fact]
    public async Task Overlapping_requests_of_the_same_employee_are_rejected()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var handler = CreateHandler(fixture);

        await handler.HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        // Both periods include their end, so a single shared day is already an overlap.
        var overlapping =
            await handler.HandleAsync(Command(fixture.EmployeeId, "2026-03-06", "2026-03-09"));

        Assert.True(overlapping.IsFailure);
        Assert.Equal(AbsenceRequestErrors.Overlapping, overlapping.Error);
        Assert.Single(await fixture.ListAsync());
    }

    /// <summary>The overlap rule is about one employee, not about the calendar.</summary>
    [Fact]
    public async Task The_same_period_is_allowed_for_another_employee()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var handler = CreateHandler(fixture);

        await handler.HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var other =
            await handler.HandleAsync(Command(fixture.OtherEmployeeId, "2026-03-02", "2026-03-06"));

        Assert.True(other.IsSuccess);
        Assert.Equal(2, (await fixture.ListAsync()).Count);
    }

    [Fact]
    public async Task A_rejected_request_no_longer_blocks_its_period()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var createHandler = CreateHandler(fixture);

        var first =
            await createHandler.HandleAsync(Command(fixture.EmployeeId, "2026-03-02",
                "2026-03-06"));
        await RejectHandler(fixture).HandleAsync(new RejectAbsenceRequestCommand(first.Value));

        var second =
            await createHandler.HandleAsync(Command(fixture.EmployeeId, "2026-03-02",
                "2026-03-06"));

        Assert.True(second.IsSuccess);
        Assert.Equal(2, (await fixture.ListAsync()).Count);
    }

    [Fact]
    public async Task Approving_an_open_request_persists_the_new_status()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var approved = await ApproveHandler(fixture)
            .HandleAsync(new ApproveAbsenceRequestCommand(created.Value));

        Assert.True(approved.IsSuccess);

        var stored = Assert.Single(await fixture.ListAsync());
        Assert.Equal(AbsenceStatus.Approved, stored.Status);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Rejecting_an_open_request_persists_the_new_status()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var rejected = await RejectHandler(fixture)
            .HandleAsync(new RejectAbsenceRequestCommand(created.Value));

        Assert.True(rejected.IsSuccess);

        var stored = Assert.Single(await fixture.ListAsync());
        Assert.Equal(AbsenceStatus.Rejected, stored.Status);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Deciding_an_unknown_request_reports_not_found()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var id = Guid.CreateVersion7();

        var result =
            await ApproveHandler(fixture).HandleAsync(new ApproveAbsenceRequestCommand(id));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotFound(id), result.Error);
    }

    /// <summary>
    ///     An open request may be edited, and the overlap check must not let it collide with
    ///     itself.
    /// </summary>
    [Fact]
    public async Task An_open_request_can_be_moved_inside_its_own_period()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var updated = await UpdateHandler(fixture).HandleAsync(
            new UpdateAbsenceRequestCommand(
                created.Value,
                AbsenceType.Training,
                new DateOnly(2026, 3, 3),
                new DateOnly(2026, 3, 7),
                "Moved by a day"));

        Assert.True(updated.IsSuccess);

        var stored = Assert.Single(await fixture.ListAsync());
        Assert.Equal(AbsenceType.Training, stored.Type);
        Assert.Equal(new DateOnly(2026, 3, 3), stored.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 7), stored.EndDate);
        Assert.Equal("Moved by a day", stored.Comment);
        Assert.Equal(AbsenceStatus.Open, stored.Status);
    }

    /// <summary>An edit must not move a request onto the period of another one.</summary>
    [Fact]
    public async Task An_edit_onto_the_period_of_another_request_is_rejected()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var createHandler = CreateHandler(fixture);

        var first =
            await createHandler.HandleAsync(Command(fixture.EmployeeId, "2026-03-02",
                "2026-03-06"));
        await createHandler.HandleAsync(Command(fixture.EmployeeId, "2026-04-01", "2026-04-03"));

        var updated = await UpdateHandler(fixture).HandleAsync(
            new UpdateAbsenceRequestCommand(
                first.Value,
                AbsenceType.Vacation,
                new DateOnly(2026, 4, 2),
                new DateOnly(2026, 4, 4),
                null));

        Assert.True(updated.IsFailure);
        Assert.Equal(AbsenceRequestErrors.Overlapping, updated.Error);
    }

    /// <summary>An approved or rejected request may no longer be edited.</summary>
    [Fact]
    public async Task A_decided_request_cannot_be_edited()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));
        await ApproveHandler(fixture).HandleAsync(new ApproveAbsenceRequestCommand(created.Value));

        var updated = await UpdateHandler(fixture).HandleAsync(
            new UpdateAbsenceRequestCommand(
                created.Value,
                AbsenceType.Training,
                new DateOnly(2026, 3, 2),
                new DateOnly(2026, 3, 6),
                null));

        Assert.True(updated.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotOpen, updated.Error);

        var stored = Assert.Single(await fixture.ListAsync());
        Assert.Equal(AbsenceType.Vacation, stored.Type);
    }

    [Fact]
    public async Task A_single_request_is_returned_with_the_name_of_its_employee()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var result = await new GetAbsenceRequestByIdHandler(fixture.Queries, fixture.Employees)
            .HandleAsync(new GetAbsenceRequestByIdQuery(created.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value, result.Value.Id);
        Assert.Equal("Anna Meier", result.Value.EmployeeName);
    }

    [Fact]
    public async Task An_unknown_request_is_reported_as_not_found()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();
        var id = Guid.CreateVersion7();

        var result = await new GetAbsenceRequestByIdHandler(fixture.Queries, fixture.Employees)
            .HandleAsync(new GetAbsenceRequestByIdQuery(id));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotFound(id), result.Error);
    }

    /// <summary>
    ///     No foreign key spans the two databases, so this is a state the absences bounded context
    ///     has to survive rather than one it can rule out.
    /// </summary>
    [Fact]
    public async Task The_list_stays_readable_when_the_employees_context_does_not_know_the_id()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));

        var handler = new GetAbsenceRequestsHandler(fixture.Queries, new FakeEmployeeDirectory());
        var result = await handler.HandleAsync(new GetAbsenceRequestsQuery());

        var stored = Assert.Single(result.Value);
        Assert.Equal(fixture.EmployeeId, stored.EmployeeId);
        Assert.Equal("Unknown employee", stored.EmployeeName);
    }

    /// <summary>
    ///     No use case dispatches an event itself. It saves, and the unit of work hands the events
    ///     of the saved request to the handlers - one per change, with the values after it.
    /// </summary>
    [Fact]
    public async Task Each_use_case_dispatches_the_event_of_its_change_when_it_saves()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));
        await UpdateHandler(fixture).HandleAsync(
            new UpdateAbsenceRequestCommand(
                created.Value,
                AbsenceType.Training,
                new DateOnly(2026, 3, 3),
                new DateOnly(2026, 3, 7),
                null));
        await ApproveHandler(fixture).HandleAsync(new ApproveAbsenceRequestCommand(created.Value));

        var filed = Period("2026-03-02", "2026-03-06");
        var moved = Period("2026-03-03", "2026-03-07");

        Assert.Equal(
            [
                new AbsenceRequestCreated(created.Value, fixture.EmployeeId, AbsenceType.Vacation,
                    filed),
                new AbsenceRequestUpdated(created.Value, fixture.EmployeeId, AbsenceType.Training,
                    moved),
                new AbsenceRequestApproved(created.Value, fixture.EmployeeId, AbsenceType.Training,
                    moved)
            ],
            fixture.DispatchedEvents);
    }

    /// <summary>A refused use case saves nothing, so nothing hears about it either.</summary>
    [Fact]
    public async Task A_refused_use_case_dispatches_nothing()
    {
        await using var fixture = await AbsencesFixture.CreateAsync();

        var created = await CreateHandler(fixture)
            .HandleAsync(Command(fixture.EmployeeId, "2026-03-02", "2026-03-06"));
        await RejectHandler(fixture).HandleAsync(new RejectAbsenceRequestCommand(created.Value));

        // The request is decided, and the second one belongs to nobody.
        var approved = await ApproveHandler(fixture)
            .HandleAsync(new ApproveAbsenceRequestCommand(created.Value));
        var unknown = await CreateHandler(fixture)
            .HandleAsync(Command(Guid.CreateVersion7(), "2026-04-01", "2026-04-03"));

        Assert.True(approved.IsFailure);
        Assert.True(unknown.IsFailure);
        Assert.Equal(
            [typeof(AbsenceRequestCreated), typeof(AbsenceRequestRejected)],
            fixture.DispatchedEvents.Select(domainEvent => domainEvent.GetType()));
    }

    private static CreateAbsenceRequestHandler CreateHandler(AbsencesFixture fixture)
    {
        return new CreateAbsenceRequestHandler(fixture.AbsenceRequests, fixture.Employees,
            fixture.DbContext, fixture.Clock);
    }

    private static UpdateAbsenceRequestHandler UpdateHandler(AbsencesFixture fixture)
    {
        return new UpdateAbsenceRequestHandler(fixture.AbsenceRequests, fixture.DbContext,
            fixture.Clock);
    }

    private static ApproveAbsenceRequestHandler ApproveHandler(AbsencesFixture fixture)
    {
        return new ApproveAbsenceRequestHandler(fixture.AbsenceRequests, fixture.DbContext,
            fixture.Clock);
    }

    private static RejectAbsenceRequestHandler RejectHandler(AbsencesFixture fixture)
    {
        return new RejectAbsenceRequestHandler(fixture.AbsenceRequests, fixture.DbContext,
            fixture.Clock);
    }

    private static CreateAbsenceRequestCommand Command(Guid employeeId, string start, string end)
    {
        return new CreateAbsenceRequestCommand(employeeId, AbsenceType.Vacation,
            DateOnly.Parse(start), DateOnly.Parse(end), null);
    }

    private static DateRange Period(string start, string end)
    {
        return DateRange.Create(DateOnly.Parse(start), DateOnly.Parse(end)).Value;
    }
}
