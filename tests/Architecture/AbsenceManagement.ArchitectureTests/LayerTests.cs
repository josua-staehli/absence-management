using ArchUnitNET.xUnitV3;
using Assembly = System.Reflection.Assembly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AbsenceManagement.ArchitectureTests.SolutionArchitecture;

namespace AbsenceManagement.ArchitectureTests;

/// <summary>
///     The layering inside a bounded context. Most of it is already true because the layers are
///     separate projects and the compiler refuses the reference that would break them. What these
///     tests add is the step before that: adding the ProjectReference in the first place now fails
///     a test.
/// </summary>
public sealed class LayerTests
{
    [Fact]
    public void The_domain_does_not_know_the_layers_above_it()
    {
        Types().That().ResideInNamespaceMatching(Namespaces.Domain)
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Namespaces.AboveTheDomain))
            .Because("the domain is the innermost layer and depends on nothing")
            .Check(Instance);
    }

    [Fact]
    public void The_application_layer_does_not_know_how_data_is_stored_or_served()
    {
        Types().That().ResideInNamespaceMatching(Namespaces.Application)
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(Namespaces.AboveTheApplication))
            .Because("use cases depend on the interfaces they declare, not on EF Core or HTTP")
            .Check(Instance);
    }

    [Fact]
    public void The_domain_references_nothing_but_the_base_class_library()
    {
        foreach (var assembly in Assemblies.Where(IsDomain))
        {
            var foreignReferences = assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .Where(name => !IsBaseClassLibrary(name) && name != "Common.Domain")
                .ToArray();

            Assert.True(
                foreignReferences.Length == 0,
                $"{assembly.GetName().Name} references {string.Join(", ", foreignReferences)}. "
                + "Business rules are plain C# and outlive whatever framework is current.");
        }
    }

    private static bool IsDomain(Assembly assembly)
    {
        return assembly.GetName().Name!.EndsWith(".Domain", StringComparison.Ordinal);
    }

    private static bool IsBaseClassLibrary(string assemblyName)
    {
        return assemblyName is "System" or "netstandard"
               || assemblyName.StartsWith("System.", StringComparison.Ordinal);
    }
}
