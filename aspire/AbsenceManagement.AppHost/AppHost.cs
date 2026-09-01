using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL runs as a container. The data volume and the persistent lifetime keep the data
// between runs, pgAdmin gives a quick look into the database at development time.
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

// One database per bounded context, named after the connection string that one asks for. Separate
// databases rather than one with two schemas: it is the cheapest way to make sure no query can
// ever join across a bounded context boundary by accident.
var employeeDatabase = postgres.AddDatabase("employeedb");
var absenceDatabase = postgres.AddDatabase("absencedb");

var api = builder.AddProject<AbsenceManagement_Api>("api")
    .WithReference(employeeDatabase)
    .WithReference(absenceDatabase)
    .WaitFor(employeeDatabase)
    .WaitFor(absenceDatabase)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http",
        _ => new ResourceUrlAnnotation { Url = "/scalar/v1", DisplayText = "API docs" });

// The frontend is one Nx workspace producing two applications, so the work they share happens
// once, in a resource of its own, before either of them starts:
//
//   1. Aspire builds the API project  -> frontend/openapi/AbsenceManagement.Api.json is rewritten
//   2. the API becomes healthy        -> "api-client" may start
//   3. `pnpm install`, then `pnpm run gen:api` regenerates the typed client, then it exits
//   4. "web" and "admin" start, both waiting for that exit code 0
var apiClient = builder.AddJavaScriptApp("api-client", "../../frontend", "gen:api")
    .WithPnpm() // `pnpm install` happens here
    .WaitFor(api);

// Both apps share the node_modules that "api-client" has installed - hence WithPnpm(install: false).
// Each gets its own Vite dev server and its own port from Aspire.
//
// The Nx workspace could also be modelled as one resource with AddNxApp from
// CommunityToolkit.Aspire.Hosting.JavaScript.Extensions - see the note in docs/BOOTSTRAP.md for
// why this stays on the official APIs.
builder.AddViteApp("web", "../../frontend") // runs `pnpm run dev` -> `nx dev web`
    .WithPnpm(install: false)
    // The Vite dev server proxies /api to this address, which avoids any CORS setup.
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitForCompletion(apiClient)
    .WithExternalHttpEndpoints();

builder.AddViteApp("admin", "../../frontend", "dev:admin")
    .WithPnpm(install: false)
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitForCompletion(apiClient)
    .WithExternalHttpEndpoints();

builder.Build().Run();
