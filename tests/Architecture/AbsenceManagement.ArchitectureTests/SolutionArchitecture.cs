using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace AbsenceManagement.ArchitectureTests;

/// <summary>
///     The solution as ArchUnitNET sees it, loaded once for the whole test run.
///     <para>
///         Nothing here names a module. Every project of the solution is called
///         <c>&lt;Owner&gt;.&lt;Layer&gt;</c>. Domain projects are referenced by a wildcard as discovery
///         anchors, while the host brings in the remaining layers. Adding a module changes no test
///         code, and forgetting to mount it in the host leaves its missing layers visible.
///     </para>
/// </summary>
internal static class SolutionArchitecture
{
    /// <summary>The owner of the shared building blocks, which is not a module.</summary>
    private const string Common = "Common";

    /// <summary>
    ///     The owner of the host, the test projects and the AppHost. The host is the composition
    ///     root and sees every module on purpose, so it is not subject to these rules.
    /// </summary>
    private const string Product = "AbsenceManagement";

    /// <summary>The layers a module or the common building blocks are split into.</summary>
    private static readonly string[] Layers =
        ["Domain", "Application", "Infrastructure", "Api", "Contracts"];

    /// <summary>
    ///     The assemblies of the solution that were found next to this one. Rules that ArchUnitNET
    ///     cannot answer (it only knows the types it loaded) are checked against these directly.
    /// </summary>
    public static IReadOnlyList<Assembly> Assemblies { get; } = LoadAssemblies();

    /// <summary>The architecture the rules are checked against.</summary>
    public static Architecture Instance { get; } =
        new ArchLoader().LoadAssemblies([.. Assemblies]).Build();

    /// <summary>The names of the loaded assemblies, used to check that no module went missing.</summary>
    public static IReadOnlyCollection<string> AssemblyNames { get; } =
        Assemblies.Select(assembly => assembly.GetName().Name!).ToArray();

    /// <summary>
    ///     The modules of the solution, derived from the assemblies that were found: whoever owns
    ///     a <c>.Domain</c> assembly is a module, except the shared building blocks.
    /// </summary>
    public static IReadOnlyList<string> Modules { get; } = AssemblyNames
        .Where(name => name.EndsWith(".Domain", StringComparison.Ordinal))
        .Select(name => name[..^".Domain".Length])
        .Where(owner => owner != Common)
        .OrderBy(owner => owner, StringComparer.Ordinal)
        .ToArray();

    private static Assembly[] LoadAssemblies()
    {
        return Directory
            .EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .Where(file => BelongsToTheSolution(Path.GetFileNameWithoutExtension(file)))
            .Select(Assembly.LoadFrom)
            .ToArray();
    }

    private static bool BelongsToTheSolution(string assemblyName)
    {
        return assemblyName.Split('.') is [var owner, var layer]
               && owner != Product
               && Layers.Contains(layer);
    }

    /// <summary>
    ///     The naming convention as regular expressions, one per layer and independent of the
    ///     module: a namespace is the assembly name, optionally followed by sub-namespaces.
    /// </summary>
    public static class Namespaces
    {
        public const string Domain = @"^\w+\.Domain(\..*)?$";
        public const string Application = @"^\w+\.Application(\..*)?$";
        public const string Infrastructure = @"^\w+\.Infrastructure(\..*)?$";
        public const string Api = @"^\w+\.Api(\..*)?$";

        public const string AboveTheDomain = @"^\w+\.(Application|Infrastructure|Api)(\..*)?$";
        public const string AboveTheApplication = @"^\w+\.(Infrastructure|Api)(\..*)?$";

        /// <summary>Everything of a module that is not its published contract.</summary>
        public static string InternalsOf(string module)
        {
            return $@"^{module}\.(Domain|Application|Infrastructure|Api)(\..*)?$";
        }

        public static string AnythingOf(string module)
        {
            return $@"^{module}(\..*)?$";
        }
    }
}
