# AGENTS.md

A web application to create and manage absences of employees. See [README.md](README.md) for the
overview and [docs/BOOTSTRAP.md](docs/BOOTSTRAP.md) for how the repository was built and why.

## Layout

```text
src/                the backend
src/Common/         building blocks every bounded context reuses
src/Contexts/       one folder per bounded context, four projects each
src/Hosts/          the web host that mounts the bounded contexts
tests/              tests for the backend
tests/Contexts/     one test project per bounded context
tests/Architecture/ rules that hold across all contexts, checked with ArchUnitNET
aspire/             the AppHost: which resources run and how they depend on each other
frontend/           the frontend built as Nx workspace, apps and packages
```

`AbsenceManagement.slnx` is the solution, in the XML `slnx` format.

## Commands

```bash
dotnet build          # also writes frontend/openapi/AbsenceManagement.Api.json
dotnet test
dotnet run --project aspire/AbsenceManagement.AppHost   # runs everything, needs Docker
```

```bash
cd frontend && pnpm check   # typecheck + oxlint (including boundaries) + formatting check
```

## Backend rules

- Warnings fail the build (`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` in
  `Directory.Build.props`). Never suppress one without a comment saying why.
- Central package management. No `Version=` in a `.csproj`, versions go into
  `Directory.Packages.props`. `TargetFramework`, `Nullable` and `ImplicitUsings` come from
  `Directory.Build.props`. Do not repeat them.
- One bounded context is one domain boundary and one physical unit: the folder
  `src/Contexts/<Name>/` with the four `<Name>.Api|Application|Infrastructure|Domain` projects.
  Folders, projects and code all say the same word - `AddBoundedContext`,
  `BoundedContextDbContext`, `BoundedContextBoundaryTests` - so there is no second vocabulary to
  translate. A new domain boundary gets its own folder, and none is ever split across two.
- Context boundary. `Absences.Application` references `Employees.Contracts` and nothing else of
  `Employees.*`. Data from another context is joined in the application layer, never in SQL. The
  two own separate databases, so there is no foreign key and no join across them.
- Business failures are values, not exceptions: return `Result` / `Result<T>` carrying an
  `Error`. `ToHttpResult()` maps `Validation` → 400, `NotFound` → 404, `Conflict` → 409.
- Handlers, repositories and queries are `internal`. Tests reach them through
  `InternalsVisibleTo`. Do not widen a type to public just to test it.
- Endpoints carry `.WithName()`, `.Produces<T>()` and `.ProducesProblems(...)`. They generate
  the OpenAPI document and therefore the TypeScript client. Return a declared response record, not
  an anonymous object.
- Adding a bounded context in `Program.cs`: the name in `AddPlaceholderConnectionStrings`,
  `Add<Name>BoundedContext()` and `Map<Name>BoundedContext()`.
- Use case tests run the real handlers and EF Core mapping against in-memory SQLite, domain tests
  need no fixture at all.
- The rules above the layering, the context boundary and the `internal` conventions are checked by
  `tests/Architecture/`. No rule there names a bounded context: they are regular expressions over
  the `<Name>.<Layer>` naming, so a new one is covered by being mounted in the host.

Migrations are generated, never written by hand:

```bash
dotnet ef migrations add <Migration> --project src/Contexts/<Name>/<Name>.Infrastructure --output-dir Persistence/Migrations
```

## Frontend rules

- Package-based Nx workspace: there is no `project.json`, tags and targets live under
  the `nx` key of each `package.json`.
- An import needs a declared dependency. A project may only import what its own `package.json`
  lists as `workspace:*`, and only what the `scope:`/`type:` tags allow (`pnpm lint`).
- `packages/shared/api-client/src/generated/` is generated. Never edit it, run `dotnet build`
  then `pnpm gen:api`. It is checked in.
- oxlint and oxfmt, not ESLint and Prettier. Nx's boundary rule runs through the experimental
  `@nx/oxlint` bridge.
- No hardcoded user-facing text. Keys live in `packages/shared/i18n`, `en.ts` is the reference
  language and the default, `de.ts` is closed with `satisfies typeof en`.
- Pages belong in `feature`, requests in `data-access`, common presentational components in
  `shared/ui`. An app is a shell: which layout, which pages.
