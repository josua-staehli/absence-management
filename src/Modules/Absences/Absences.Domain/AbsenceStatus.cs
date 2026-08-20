namespace Absences.Domain;

/// <summary>
///     Where an absence request stands. A request starts as <see cref="Open" /> and is decided
///     exactly once, see <see cref="AbsenceRequest" />.
/// </summary>
public enum AbsenceStatus
{
    Open = 1,
    Approved = 2,
    Rejected = 3
}
