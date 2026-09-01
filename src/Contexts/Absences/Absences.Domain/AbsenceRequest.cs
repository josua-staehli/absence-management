using Common.Domain.Primitives;
using Common.Domain.Results;

namespace Absences.Domain;

/// <summary>
///     Aggregate root of an absence request. Every state change goes through a method on this
///     class, so the business rules cannot be bypassed by the API, the application layer or
///     EF Core.
///     <para>
///         The employee appears as a plain id: employees are a bounded context of their own, so
///         this aggregate can only carry the reference, never the employee itself. Whether that id
///         belongs to somebody is asked once, in the use case, across the bounded context boundary.
///     </para>
/// </summary>
public sealed class AbsenceRequest : AggregateRoot<Guid>
{
    public const int MaxCommentLength = 500;

    private AbsenceRequest(
        Guid id,
        Guid employeeId,
        AbsenceType type,
        DateRange period,
        string? comment,
        DateTimeOffset createdAt) : base(id)
    {
        EmployeeId = employeeId;
        Type = type;
        Period = period;
        Comment = comment;
        Status = AbsenceStatus.Open;
        CreatedAt = createdAt;
    }

    /// <summary>Used by EF Core for materialization.</summary>
    private AbsenceRequest()
    {
        Period = null!;
    }

    public Guid EmployeeId { get; private set; }

    public AbsenceType Type { get; private set; }

    public DateRange Period { get; private set; }

    public AbsenceStatus Status { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    ///     Deciding and editing both come down to this question: only an open request allows
    ///     either.
    /// </summary>
    public bool IsOpen => Status == AbsenceStatus.Open;

    /// <summary>
    ///     The only way to create a request: it needs an employee and a valid absence type, and it
    ///     always starts as <see cref="AbsenceStatus.Open" />.
    /// </summary>
    public static Result<AbsenceRequest> Create(
        Guid employeeId,
        AbsenceType type,
        DateRange period,
        string? comment,
        DateTimeOffset now)
    {
        if (employeeId == Guid.Empty) return AbsenceRequestErrors.EmployeeRequired;

        if (!Enum.IsDefined(type)) return AbsenceRequestErrors.TypeRequired;

        var normalizedComment = NormalizeComment(comment);
        if (normalizedComment.IsFailure) return normalizedComment.Error;

        return new AbsenceRequest(Guid.CreateVersion7(), employeeId, type, period,
            normalizedComment.Value, now);
    }

    /// <summary>
    ///     An open request may be edited, an approved or rejected one may not. The employee is not
    ///     among the changeable values, a request for somebody else is a different request.
    /// </summary>
    public Result Update(AbsenceType type, DateRange period, string? comment, DateTimeOffset now)
    {
        if (!IsOpen) return AbsenceRequestErrors.NotOpen;

        if (!Enum.IsDefined(type)) return AbsenceRequestErrors.TypeRequired;

        var normalizedComment = NormalizeComment(comment);
        if (normalizedComment.IsFailure) return normalizedComment.Error;

        Type = type;
        Period = period;
        Comment = normalizedComment.Value;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>Only an open request may be approved, and it may be approved only once.</summary>
    public Result Approve(DateTimeOffset now)
    {
        return Decide(AbsenceStatus.Approved, now);
    }

    /// <summary>
    ///     The same in the other direction. A rejected request is not an absence, so it stops
    ///     blocking its period for the overlap check.
    /// </summary>
    public Result Reject(DateTimeOffset now)
    {
        return Decide(AbsenceStatus.Rejected, now);
    }

    /// <summary>
    ///     What both decisions have in common: either can be made only while the request is open.
    /// </summary>
    private Result Decide(AbsenceStatus decision, DateTimeOffset now)
    {
        if (!IsOpen) return AbsenceRequestErrors.NotOpen;

        Status = decision;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    ///     The comment is optional, so whitespace and an empty text mean the same as none at all.
    ///     Normalizing here keeps values that only differ in padding out of the aggregate.
    /// </summary>
    private static Result<string?> NormalizeComment(string? comment)
    {
        var trimmed = comment?.Trim();

        if (string.IsNullOrEmpty(trimmed)) return Result.Success<string?>(null);

        return trimmed.Length > MaxCommentLength
            ? AbsenceRequestErrors.CommentTooLong
            : Result.Success<string?>(trimmed);
    }
}
