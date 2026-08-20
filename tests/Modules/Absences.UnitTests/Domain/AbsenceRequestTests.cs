using Absences.Domain;
using Common.Domain.Results;

namespace Absences.UnitTests.Domain;

/// <summary>
///     The rules of the aggregate, all of them testable without a database, because the aggregate
///     has no dependencies.
/// </summary>
public class AbsenceRequestTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     The start date must not be after the end date. The value object enforces it, so the
    ///     aggregate never sees an invalid period.
    /// </summary>
    [Fact]
    public void A_period_that_ends_before_it_starts_is_invalid()
    {
        var period = DateRange.Create(new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 5));

        Assert.True(period.IsFailure);
        Assert.Equal(AbsenceRequestErrors.EndDateBeforeStartDate, period.Error);
    }

    [Fact]
    public void A_period_of_a_single_day_is_valid()
    {
        var day = new DateOnly(2026, 3, 5);

        var period = DateRange.Create(day, day);

        Assert.True(period.IsSuccess);
        Assert.Equal(day, period.Value.Start);
        Assert.Equal(day, period.Value.End);
    }

    /// <summary>A request is created with the status open.</summary>
    [Fact]
    public void A_new_request_is_open()
    {
        var request = CreateRequest();

        Assert.True(request.IsSuccess);
        Assert.Equal(AbsenceStatus.Open, request.Value.Status);
        Assert.Equal(Now, request.Value.CreatedAt);
        Assert.Null(request.Value.UpdatedAt);
    }

    /// <summary>A request must not be created without an employee.</summary>
    [Fact]
    public void A_request_without_an_employee_is_invalid()
    {
        var request = AbsenceRequest.Create(Guid.Empty, AbsenceType.Vacation, Period(), null, Now);

        Assert.True(request.IsFailure);
        Assert.Equal(AbsenceRequestErrors.EmployeeRequired, request.Error);
    }

    /// <summary>
    ///     A request must have an absence type. The enum is only a suggestion: the value on the
    ///     wire can be anything.
    /// </summary>
    [Fact]
    public void A_request_with_an_unknown_type_is_invalid()
    {
        var request = AbsenceRequest.Create(EmployeeId, (AbsenceType)99, Period(), null, Now);

        Assert.True(request.IsFailure);
        Assert.Equal(AbsenceRequestErrors.TypeRequired, request.Error);
    }

    /// <summary>Only an open request may be approved.</summary>
    [Fact]
    public void An_open_request_can_be_approved()
    {
        var request = CreateRequest().Value;

        var result = request.Approve(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(AbsenceStatus.Approved, request.Status);
        Assert.Equal(Now, request.UpdatedAt);
    }

    /// <summary>Only an open request may be rejected.</summary>
    [Fact]
    public void An_open_request_can_be_rejected()
    {
        var request = CreateRequest().Value;

        var result = request.Reject(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(AbsenceStatus.Rejected, request.Status);
        Assert.Equal(Now, request.UpdatedAt);
    }

    /// <summary>An approved request must not be rejected afterward.</summary>
    [Fact]
    public void An_approved_request_cannot_be_rejected()
    {
        var request = CreateRequest().Value;
        request.Approve(Now);

        var result = request.Reject(Now.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotOpen, result.Error);
        Assert.Equal(AbsenceStatus.Approved, request.Status);
        Assert.Equal(Now, request.UpdatedAt);
    }

    /// <summary>An approved request must not be approved a second time.</summary>
    [Fact]
    public void An_approved_request_cannot_be_approved_again()
    {
        var request = CreateRequest().Value;
        request.Approve(Now);

        var result = request.Approve(Now.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotOpen, result.Error);
        Assert.Equal(Now, request.UpdatedAt);
    }

    /// <summary>A rejected request must not be approved afterward.</summary>
    [Fact]
    public void A_rejected_request_cannot_be_approved()
    {
        var request = CreateRequest().Value;
        request.Reject(Now);

        var result = request.Approve(Now.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotOpen, result.Error);
        Assert.Equal(AbsenceStatus.Rejected, request.Status);
    }

    /// <summary>An open request may be edited.</summary>
    [Fact]
    public void An_open_request_can_be_edited()
    {
        var request = CreateRequest().Value;
        var period = DateRange.Create(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3)).Value;

        var result = request.Update(AbsenceType.Training, period, "Conference", Now.AddDays(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(AbsenceType.Training, request.Type);
        Assert.Equal(period, request.Period);
        Assert.Equal("Conference", request.Comment);
        Assert.Equal(AbsenceStatus.Open, request.Status);
        Assert.Equal(Now.AddDays(1), request.UpdatedAt);
    }

    /// <summary>An approved or rejected request may no longer be edited.</summary>
    [Fact]
    public void A_decided_request_can_no_longer_be_edited()
    {
        var request = CreateRequest().Value;
        request.Approve(Now);

        var result = request.Update(AbsenceType.Training, Period(), null, Now.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(AbsenceRequestErrors.NotOpen, result.Error);
        Assert.Equal(AbsenceType.Vacation, request.Type);
    }

    [Fact]
    public void A_comment_longer_than_the_maximum_is_rejected()
    {
        var request = AbsenceRequest.Create(
            EmployeeId,
            AbsenceType.Vacation,
            Period(),
            new string('x', AbsenceRequest.MaxCommentLength + 1),
            Now);

        Assert.True(request.IsFailure);
        Assert.Equal(AbsenceRequestErrors.CommentTooLong, request.Error);
    }

    /// <summary>A comment is optional, so a blank one is the same as none at all.</summary>
    [Fact]
    public void A_blank_comment_is_stored_as_none()
    {
        var request = AbsenceRequest.Create(EmployeeId, AbsenceType.Vacation, Period(), "   ", Now);

        Assert.True(request.IsSuccess);
        Assert.Null(request.Value.Comment);
    }

    private static DateRange Period()
    {
        return DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6)).Value;
    }

    private static Result<AbsenceRequest> CreateRequest()
    {
        return AbsenceRequest.Create(EmployeeId, AbsenceType.Vacation, Period(), null, Now);
    }
}
