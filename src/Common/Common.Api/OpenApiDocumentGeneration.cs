using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Common.Api;

/// <summary>
///     Support for the build-time OpenAPI document generation
///     (<c>Microsoft.Extensions.ApiDescription.Server</c>, see the API project's csproj).
///     To read the routes, the tool starts the host in a process of its own and stops it right
///     after <c>builder.Build()</c>. Nothing is ever served and no connection is ever opened - but
///     the bounded contexts still run their registration, and those fail fast when a connection
///     string is missing. That fail-fast is worth keeping, so the host hands out a placeholder
///     instead of weakening the check.
/// </summary>
public static class OpenApiDocumentGeneration
{
    /// <summary>The tool hosts the application inside an assembly with this name.</summary>
    private const string ToolAssemblyName = "GetDocument.Insider";

    /// <summary>
    ///     Never opened - it only has to get past the bounded contexts' "is it configured?" check
    ///     and be parseable by the provider chosen in <c>Common.Infrastructure</c>.
    /// </summary>
    private const string PlaceholderConnectionString =
        "Host=openapi-document-generation;Database=none;Username=none;Password=none";

    /// <summary>True while <c>dotnet build</c> is generating the OpenAPI document.</summary>
    public static bool IsRunning =>
        Assembly.GetEntryAssembly()?.GetName().Name == ToolAssemblyName;

    /// <summary>
    ///     Registers a placeholder for each of the given connection string names, but only while
    ///     the document is being generated. At every other time the configuration is untouched and
    ///     a missing connection string is still an error on start up.
    /// </summary>
    public static IHostApplicationBuilder AddPlaceholderConnectionStrings(
        this IHostApplicationBuilder builder,
        params string[] connectionStringNames)
    {
        if (!IsRunning) return builder;

        builder.Configuration.AddInMemoryCollection(
            connectionStringNames.Select(name =>
                new KeyValuePair<string, string?>(
                    $"ConnectionStrings:{name}",
                    PlaceholderConnectionString)));

        return builder;
    }
}
