using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AbsenceManagement.ArchitectureTests.SolutionArchitecture;

namespace AbsenceManagement.ArchitectureTests;

/// <summary>
///     The rule the modular monolith stands or falls with: a module is reached through its
///     published contract, never through its inside. The pairs are derived from the modules that
///     were found, so a third module is checked against the other two without a line being added
///     here.
/// </summary>
public sealed class ModuleBoundaryTests
{
    public static TheoryData<string, string> ModulePairs()
    {
        var pairs = new TheoryData<string, string>();

        foreach (var consumer in Modules)
        {
            foreach (var other in Modules.Where(module => module != consumer))
            {
                pairs.Add(consumer, other);
            }
        }

        return pairs;
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void A_module_reaches_another_one_only_through_its_contracts(
        string consumer, string other)
    {
        Types().That().ResideInNamespaceMatching(Namespaces.AnythingOf(consumer))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Namespaces.InternalsOf(other)))
            .Because($"{consumer} may see {other}.Contracts and nothing else of {other}")
            .Check(Instance);
    }

    /// <summary>
    ///     Domain projects are loaded independently of the host. Without this check, a module that
    ///     is not mounted in the host would contribute its domain types but silently skip the rules
    ///     for its other layers.
    /// </summary>
    [Fact]
    public void Every_module_is_loaded_with_all_of_its_layers()
    {
        Assert.NotEmpty(Modules);

        foreach (var module in Modules)
        {
            foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
            {
                Assert.Contains($"{module}.{layer}", AssemblyNames);
            }
        }
    }
}
