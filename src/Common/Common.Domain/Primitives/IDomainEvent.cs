namespace Common.Domain.Primitives;

/// <summary>
///     Something that happened to an aggregate, named in the past tense:
///     <c>AbsenceRequestApproved</c>, not <c>ApproveAbsenceRequest</c>. The aggregate raises it in
///     the same method that makes the change, and the unit of work hands it to its handlers when it
///     saves that change.
///     <para>
///         An event is a fact, so it is an immutable record. It carries the values a handler needs
///         to react, so that no handler has to load the aggregate again.
///     </para>
/// </summary>
public interface IDomainEvent;
