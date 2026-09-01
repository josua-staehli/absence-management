using Common.Application;

namespace Employees.Application;

/// <summary>
///     The transaction boundary of this bounded context. Each one gets its own marker interface so
///     that a handler can never accidentally save through another one's <c>DbContext</c>.
/// </summary>
public interface IEmployeesUnitOfWork : IUnitOfWork;
