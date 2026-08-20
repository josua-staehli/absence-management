using Common.Domain.Results;

namespace Absences.Domain;

/// <summary>
///     Value object for a period of whole days, both ends included. The start must not be after
///     the end, and this is where that is enforced: a <see cref="DateRange" /> cannot exist in an
///     invalid state, so no code that holds one has to check it again.
/// </summary>
public sealed record DateRange
{
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        return end < start
            ? AbsenceRequestErrors.EndDateBeforeStartDate
            : new DateRange(start, end);
    }
}
