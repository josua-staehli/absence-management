using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AbsenceManagement.ArchitectureTests.SolutionArchitecture;

namespace AbsenceManagement.ArchitectureTests;

/// <summary>
///     Where a kind of type lives, and who gets to see it. A bounded context's surface is its
///     registration methods and its contracts, everything else is `internal`.
/// </summary>
public sealed class ConventionTests
{
    [Fact]
    public void Handlers_are_internal_and_live_in_the_application_layer()
    {
        Classes().That().HaveNameEndingWith("Handler")
            .Should().BeInternal()
            .AndShould().BeSealed()
            .AndShould().ResideInNamespaceMatching(Namespaces.Application)
            .Because("callers depend on ICommandHandler / IQueryHandler, not on the handler")
            .Check(Instance);
    }

    [Fact]
    public void Repositories_and_queries_are_internal_and_live_in_the_infrastructure_layer()
    {
        // The interfaces of the same name are public and stay in the application layer, which is
        // why this only looks at classes.
        Classes().That().HaveNameEndingWith("Repository").Or().HaveNameEndingWith("Queries")
            .Should().BeInternal()
            .AndShould().ResideInNamespaceMatching(Namespaces.Infrastructure)
            .Because("how data is read and written is the bounded context's own business")
            .Check(Instance);
    }

    [Fact]
    public void Endpoints_are_internal_and_live_in_the_api_layer()
    {
        Classes().That().HaveNameEndingWith("Endpoints")
            .Should().BeInternal()
            .AndShould().ResideInNamespaceMatching(Namespaces.Api)
            .Because(
                "the host mounts a bounded context through its Map...BoundedContext method, "
                + "not per endpoint")
            .Check(Instance);
    }
}
