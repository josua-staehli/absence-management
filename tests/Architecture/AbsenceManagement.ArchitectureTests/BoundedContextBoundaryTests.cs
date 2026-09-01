using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AbsenceManagement.ArchitectureTests.SolutionArchitecture;

namespace AbsenceManagement.ArchitectureTests;

/// <summary>
///     The rule the modular monolith stands or falls with: a bounded context is reached through
///     its published contract, never through its inside. The pairs are derived from the bounded
///     contexts that were found, so a third one is checked against the other two without a line
///     being added here.
/// </summary>
public sealed class BoundedContextBoundaryTests
{
    public static TheoryData<string, string> BoundedContextPairs()
    {
        var pairs = new TheoryData<string, string>();

        foreach (var consumer in BoundedContexts)
        {
            foreach (var other in BoundedContexts.Where(name => name != consumer))
            {
                pairs.Add(consumer, other);
            }
        }

        return pairs;
    }

    [Theory]
    [MemberData(nameof(BoundedContextPairs))]
    public void A_bounded_context_reaches_another_one_only_through_its_contracts(
        string consumer, string other)
    {
        Types().That().ResideInNamespaceMatching(Namespaces.AnythingOf(consumer))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Namespaces.InternalsOf(other)))
            .Because($"{consumer} may see {other}.Contracts and nothing else of {other}")
            .Check(Instance);
    }

    /// <summary>
    ///     Domain projects are loaded independently of the host. Without this check, a bounded
    ///     context that is not mounted in the host would contribute its domain types but silently
    ///     skip the rules for its other layers.
    /// </summary>
    [Fact]
    public void Every_bounded_context_is_loaded_with_all_of_its_layers()
    {
        Assert.NotEmpty(BoundedContexts);

        foreach (var boundedContext in BoundedContexts)
        {
            foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
            {
                Assert.Contains($"{boundedContext}.{layer}", AssemblyNames);
            }
        }
    }
}
