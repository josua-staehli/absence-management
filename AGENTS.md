# AGENTS.md

A web application to create and manage absences of employees. See [README.md](README.md) for the
overview and [docs/BOOTSTRAP.md](docs/BOOTSTRAP.md) for how the repository was built and why.

## Layout

```text
src/               the backend
src/Common/        building blocks every DDD module reuses
src/Modules/       modules fitting into the DDD pattern (bounded contexts)
src/Host/          the web host that mounts the modules
tests/             tests for the backend
tests/Modules/     one test project per module
aspire/            the AppHost: which resources run and how they depend on each other
frontend/          the frontend built as Nx workspace, apps and packages
```

`AbsenceManagement.slnx` is the solution, in the XML `slnx` format.

## Commands

```bash
dotnet build          # also writes frontend/openapi/AbsenceManagement.Api.json
dotnet test
dotnet run --project aspire/AbsenceManagement.AppHost   # runs everything, needs Docker
```

```bash
cd frontend && pnpm check   # typecheck + oxlint + boundaries + formatting check
```

## Backend rules

- Warnings fail the build (`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` in
  `Directory.Build.props`). Never suppress one without a comment saying why.
- Central package management. No `Version=` in a `.csproj`, versions go into
  `Directory.Packages.props`. `TargetFramework`, `Nullable` and `ImplicitUsings` come from
  `Directory.Build.props`. Do not repeat them.
- Module boundary. `Absences.Application` references `Employees.Contracts` and nothing else of
  `Employees.*`. Cross-module data is joined in the application layer, never in SQL. The two
  modules own separate databases, so there is no foreign key and no join across them.
- Business failures are values, not exceptions: return `Result` / `Result<T>` carrying an
  `Error`. `ToHttpResult()` maps `Validation` → 400, `NotFound` → 404, `Conflict` → 409.
- Handlers, repositories and queries are `internal`. Tests reach them through
  `InternalsVisibleTo`. Do not widen a type to public just to test it.
- Endpoints carry `.WithName()`, `.Produces<T>()` and `.ProducesProblems(...)`. They generate
  the OpenAPI document and therefore the TypeScript client. Return a declared response record, not
  an anonymous object.
- Adding a module in `Program.cs`: the name in
  `AddPlaceholderConnectionStrings`, `Add<Name>Module()` and `Map<Name>Module()`.
- Use case tests run the real handlers and EF Core mapping against in-memory SQLite, domain tests
  need no fixture at all.

Migrations are generated, never written by hand:

```bash
dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure --output-dir Persistence/Migrations
```

## Frontend rules

- Package-based Nx workspace: there is no `project.json`, tags and targets live under
  the `nx` key of each `package.json`.
- An import needs a declared dependency. A project may only import what its own `package.json`
  lists as `workspace:*`, and only what the `scope:`/`type:` tags allow (`pnpm boundaries`).
- `packages/shared/api-client/src/generated/` is generated. Never edit it, run `dotnet build`
  then `pnpm gen:api`. It is checked in.
- oxlint and oxfmt, not ESLint and Prettier. ESLint exists for the one boundary rule only.
- No hardcoded user-facing text. Keys live in `packages/shared/i18n`, `en.ts` is the reference
  language and the default, `de.ts` is closed with `satisfies typeof en`.
- Pages belong in `feature`, requests in `data-access`, common presentational components in
  `shared/ui`. An app is a shell: which layout, which pages.
