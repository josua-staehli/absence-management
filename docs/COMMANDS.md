# Command Cheat Sheet

The commands that come up while working on this repository. Run commands from the repository root
unless noted otherwise.

See [README.md](../README.md) for the overview, [AGENTS.md](../AGENTS.md) for conventions, and
[BOOTSTRAP.md](BOOTSTRAP.md) for setup details.

## First-time setup

Requires the .NET 10 SDK, Node 22.12 or newer, and a running container runtime.

```bash
corepack enable       # Use the pnpm version declared by the repository
dotnet tool restore   # Restore dotnet-ef
```

Aspire installs frontend dependencies automatically. For frontend-only work, run this from
`frontend/`:

```bash
pnpm install
pnpm exec playwright install   # Only required for pnpm e2e
```

## Run the application

```bash
dotnet run --project aspire/AbsenceManagement.AppHost
aspire run   # Equivalent when the optional Aspire CLI is installed
```

Aspire starts PostgreSQL, pgAdmin, the API, client generation, and both frontends. Application
ports change per run, find them in the dashboard at <http://localhost:15246>.

To run individual parts:

```bash
dotnet run --project src/Host/AbsenceManagement.Api   # API: http://localhost:5180/scalar/v1
```

From `frontend/` (with the standalone API available):

```bash
pnpm dev         # Employee app: http://localhost:4200
pnpm dev:admin   # Approver app: http://localhost:4201
pnpm storybook   # Shared UI: http://localhost:4400
```

## Backend

```bash
dotnet build
dotnet test
dotnet build src/Modules/Absences/Absences.Application
dotnet test tests/Modules/Absences.UnitTests
dotnet test --filter "FullyQualifiedName~AbsenceTests"
dotnet format --verify-no-changes
dotnet format   # Apply formatting
```

Build output goes to `artifacts/`.

### Packages and projects

Package versions belong in `Directory.Packages.props`, not in project files.

```bash
dotnet add src/Modules/Absences/Absences.Application package <PackageId>
dotnet add src/Modules/Absences/Absences.Api reference src/Modules/Absences/Absences.Infrastructure
dotnet sln AbsenceManagement.slnx add <project-path>
dotnet list package --outdated
```

### Entity Framework migrations

Generate migrations, never write them by hand. Replace the module in the project path as needed.

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/Absences/Absences.Infrastructure \
  --output-dir Persistence/Migrations
dotnet ef migrations list --project src/Modules/Absences/Absences.Infrastructure
dotnet ef migrations remove --project src/Modules/Absences/Absences.Infrastructure
```

Each module applies its own migrations at startup. `migrations remove` is only for the latest
unapplied migration. `dotnet ef database update` is not part of the normal workflow.

## Frontend

Run these commands from `frontend/`.

| Command | Purpose |
| --- | --- |
| `pnpm check` | Typecheck, lint, boundaries, and formatting check |
| `pnpm test` | Run Vitest tests |
| `pnpm e2e` | Run Playwright tests |
| `pnpm build` | Generate the API client and build both apps |
| `pnpm format` | Apply oxfmt formatting |
| `pnpm lint:fix` | Apply safe oxlint fixes |
| `pnpm graph` | Open the Nx project graph |

Useful focused commands:

```bash
pnpm format && pnpm lint:fix && pnpm check
pnpm exec nx test @absence-management/absences-feature
pnpm exec nx run-many -t test --projects=@absence-management/absences-*
pnpm exec nx affected -t test typecheck
pnpm exec nx show projects
pnpm exec nx sync    # Refresh TypeScript project references
pnpm exec nx reset   # Clear stale cache and daemon state
```

Add a dependency to the package that imports it. Use the workspace root only for shared tooling:

```bash
pnpm add <package> --filter @absence-management/web
pnpm add -w -D <tooling-package>
```

### Regenerate the API client

Never edit `frontend/packages/shared/api-client/src/generated/` manually. After changing an
endpoint or contract, run this from the repository root:

```bash
dotnet build
cd frontend && pnpm gen:api && pnpm check
```

Aspire performs both generation steps automatically when it starts.

## Reset the development database

PostgreSQL data persists in a Docker volume. To start clean, stop the container shown by
`docker ps`, then remove its volume:

```bash
docker ps
docker volume ls
docker stop <postgres-container>
docker volume rm <volume-name>
```

This permanently deletes the development data in that volume.

## Update .NET

For a patch or feature-band update within .NET 10, update the installed SDK. `global.json` uses
`latestFeature`. For a major update, change these together:

1. Installed SDK, then `global.json`.
2. `TargetFramework` in `Directory.Build.props`.
3. Runtime-related versions in `Directory.Packages.props`.
4. `dotnet-ef` in `dotnet-tools.json`.

Check and verify with:

```bash
dotnet --list-sdks
dotnet --version
dotnet list package --outdated
dotnet build && dotnet test
```

## Troubleshooting

| Symptom | Try |
| --- | --- |
| Frontend types do not match the API | From root: `dotnet build` / from `frontend/`: `pnpm gen:api` |
| Typecheck fails on a project reference | `pnpm exec nx sync` |
| Nx returns stale results | `pnpm exec nx reset` |
| `Cannot find module '@absence-management/…'` | Add the dependency to the importing project |
| `dotnet ef` is unavailable | `dotnet tool restore` |
| SDK is missing or mismatched | Compare `dotnet --list-sdks` with `global.json` |
| Build fails on a warning | Fix it. Suppress only with a comment explaining why |
