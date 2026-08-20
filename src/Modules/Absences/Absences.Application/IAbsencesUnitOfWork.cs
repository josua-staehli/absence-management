using Common.Application;

namespace Absences.Application;

/// <summary>
///     The transaction boundary of this module. Each module gets its own marker interface so that a
///     handler can never accidentally save through another module's <c>DbContext</c>.
/// </summary>
public interface IAbsencesUnitOfWork : IUnitOfWork;
