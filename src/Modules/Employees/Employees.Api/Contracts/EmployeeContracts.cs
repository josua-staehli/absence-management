namespace Employees.Api.Contracts;

/// <summary>
///     Request model of the HTTP API. It is kept separate from the application command, so the
///     public contract can evolve independently of the internal use case.
/// </summary>
public sealed record CreateEmployeeRequest(string FirstName, string LastName, string Email);

/// <summary>
///     Body of the <c>201 Created</c> response. A named type, not an anonymous object, so it
///     appears in the OpenAPI document a frontend client is generated from.
/// </summary>
public sealed record CreateEmployeeResponse(Guid Id);
