# Command Cheat Sheet

The commands that come up while working on this repository. Run commands from the repository root
unless noted otherwise.

See [README.md](../README.md) for the overview, [AGENTS.md](../AGENTS.md) for conventions, and
[BOOTSTRAP.md](BOOTSTRAP.md) for setup details.

## First-time setup

Install [mise](https://mise.jdx.dev) and a container runtime for PostgreSQL. mise installs the .NET
SDK and Node; Node's bundled Corepack downloads the project's pnpm version on first use.

### Install mise

**Windows.** mise comes from Scoop. If Scoop is not installed yet, install it first, from a
**non-admin** PowerShell — it lands in `%USERPROFILE%\scoop` and needs no elevation:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
irm get.scoop.sh | iex
```

```powershell
scoop install mise
```

`winget install jdx.mise` works too. Scoop is preferred because it stays inside the user profile
and `scoop update mise` upgrades in place.

**macOS:**

```bash
brew install mise
```

**Linux:**

```bash
curl -fsSL https://mise.run | sh
```

The `mise.run` script installs to `~/.local/bin` and works on macOS without Homebrew as well.
Ubuntu, Fedora, Arch and Alpine also package mise, see
[installing mise](https://mise.jdx.dev/installing-mise.html).

### Activate mise in the shell

Activation is what makes entering the repository switch the tool versions. Add the matching line to
the shell profile, then open a new shell:

```bash
# PowerShell, in $PROFILE
(&mise activate pwsh) | Out-String | Invoke-Expression

# zsh, in ~/.zshrc
eval "$(mise activate zsh)"

# bash, in ~/.bashrc
eval "$(mise activate bash)"

# fish, in ~/.config/fish/config.fish
mise activate fish | source
```

`$PROFILE` may not exist yet; `New-Item -ItemType File -Path $PROFILE -Force` creates it.

### Install the tools

From the repository root:

```bash
mise trust            # Confirm this repository's mise.toml, once per clone
mise install          # Install the SDK and Node, and enable Corepack's pnpm shims
dotnet tool restore   # Restore dotnet-ef
```

Verify:

```bash
mise doctor      # Activation, shims and settings
mise ls          # Which versions are active, and where they come from
dotnet --version
node --version
docker version
```

Check pnpm from `frontend/`, so Corepack finds the project's `packageManager` field:

```bash
cd frontend
pnpm --version
```

Aspire installs frontend dependencies automatically. For frontend-only work, run this from
`frontend/`:

```bash
pnpm install
pnpm exec playwright install   # Only required for pnpm e2e
```

## Tool versions

Each tool has one version declaration. mise reads .NET's `global.json`, declares Node in
`mise.toml`, and enables Corepack to read pnpm's `packageManager` field from `frontend/`:

| Tool     | Version lives in                           | Bump it with              |
| -------- | ------------------------------------------ | ------------------------- |
| .NET SDK | `global.json` → `sdk.version`              | Edit, then `mise install` |
| Node     | `mise.toml` → `node`                       | `mise lock --bump node`   |
| pnpm     | `frontend/package.json` → `packageManager` | `corepack up`             |

Run pnpm update commands from `frontend/`; see [Update pnpm](#update-pnpm).

`mise.lock` records the versions those requests resolved to. It, `mise.toml` and `global.json` are
committed.

```bash
mise ls                  # Active versions and the file each one comes from
mise ls --current        # Only what applies to the current directory
mise install             # Install every tool the current directory resolves to
mise exec -- dotnet --list-sdks   # Run a command in mise's environment without activation
mise lock                # Refresh lock metadata, preserving matching versions
mise lock --bump node    # Move Node to the newest version its request allows
mise doctor              # Diagnose activation and configuration problems
```

mise updates itself through whatever installed it — `scoop update mise`, `brew upgrade mise`, or
`mise self-update` for the `mise.run` install.

Personal deviations belong in `mise.local.toml`; it and its generated `mise.local.lock` are
git-ignored:

```toml
[tools]
node = { version = '24', postinstall = 'corepack enable' }
```

## JetBrains IDEs

Install the [Mise plugin](https://plugins.jetbrains.com/plugin/24904-mise) (Rider 2026.1+ and
WebStorm 2026.1+): <kbd>Settings</kbd> → <kbd>Plugins</kbd> → <kbd>Marketplace</kbd> → search
for `Mise`. It reads `mise.toml` and hands the IDE the same tools the shell gets.

The project side is already committed in `.idea/.idea.AbsenceManagement/.idea/mise.xml`, so after
installing the plugin and reopening the solution the following are on under
<kbd>Settings</kbd> → <kbd>Tools</kbd> → <kbd>Mise Settings</kbd>:

| Setting                                 | Why                                              |
| --------------------------------------- | ------------------------------------------------ |
| Use environment variables from mise     | Master switch, and where `DOTNET_ROOT` comes from |
| Use in run configurations               | The AppHost and the test runners get mise's `PATH` |
| Use in Nx commands                      | Nx Console runs `pnpm` and `nx` from mise's Node |
| Use in all other command line execution | The built-in terminal and external tools, too    |

The Node interpreter and the package manager are then set from `mise.toml` automatically
(<kbd>Settings</kbd> → <kbd>Languages & Frameworks</kbd> → <kbd>Node.js</kbd>).

Rider resolves its .NET toolset separately from run configurations. If it reports a missing or
wrong SDK, point it at mise's installation once under <kbd>Settings</kbd> →
<kbd>Build, Execution, Deployment</kbd> → <kbd>Toolset and Build</kbd> → *.NET CLI executable
path*. The path is what mise reports here:

```bash
mise which dotnet
```

Launching the IDE from a shell where mise is activated avoids that step, because the IDE then
inherits `PATH` and `DOTNET_ROOT`. Restart the IDE after changing either.

## Run the application

```bash
dotnet run --project aspire/AbsenceManagement.AppHost
aspire run   # Equivalent when the optional Aspire CLI is installed
```

Aspire starts PostgreSQL, pgAdmin, the API, client generation, and both frontends. Application
ports change per run, find them in the dashboard at <http://localhost:15246>.

To run individual parts:

```bash
dotnet run --project src/Hosts/AbsenceManagement.Api   # API: http://localhost:5180/scalar/v1
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
dotnet build src/Contexts/Absences/Absences.Application
dotnet test tests/Contexts/Absences.UnitTests
dotnet test --filter "FullyQualifiedName~AbsenceTests"
dotnet format --verify-no-changes
dotnet format   # Apply formatting
```

Build output goes to `artifacts/`.

### Packages and projects

Package versions belong in `Directory.Packages.props`, not in project files.

```bash
dotnet add src/Contexts/Absences/Absences.Application package <PackageId>
dotnet add src/Contexts/Absences/Absences.Api reference src/Contexts/Absences/Absences.Infrastructure
dotnet sln AbsenceManagement.slnx add <project-path>
dotnet list package --outdated
```

### Entity Framework migrations

Generate migrations, never write them by hand. Replace the bounded context in the project path as
needed.

```bash
dotnet ef migrations add <Name> \
  --project src/Contexts/Absences/Absences.Infrastructure \
  --output-dir Persistence/Migrations
dotnet ef migrations list --project src/Contexts/Absences/Absences.Infrastructure
dotnet ef migrations remove --project src/Contexts/Absences/Absences.Infrastructure
```

Each bounded context applies its own migrations at startup. `migrations remove` is only for the
latest unapplied migration. `dotnet ef database update` is not part of the normal workflow.

## Frontend

Run these commands from `frontend/`.

| Command | Purpose |
| --- | --- |
| `pnpm check` | Typecheck, lint (including Nx boundaries), and formatting check |
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

The SDK version lives in `global.json` only. mise reads `sdk.version` from it and installs exactly
that, so a patch or feature-band update within .NET 10 is one edit:

```jsonc
{ "sdk": { "rollForward": "latestPatch", "version": "10.0.401" } }
```

```bash
mise install   # Installs the new SDK and updates mise.lock
```

Use a published, complete SDK version in `global.json`; the JSON above is an example. Keep
`dotnet` out of `mise.toml` so there is only one declaration.

mise installs `sdk.version` verbatim. .NET then applies `rollForward`: `latestPatch` selects the
highest installed patch in that feature band, even if the requested version is installed. The
lockfile therefore pins the SDK installation, but does not guarantee the exact SDK used on a
machine with additional patches. Use `disable` if exact SDK selection is required. See
[mise's .NET integration](https://mise.jdx.dev/lang/dotnet.html) and
[.NET SDK selection](https://learn.microsoft.com/dotnet/core/tools/global-json#rollforward).

For a major update, change these together:

1. `sdk.version` in `global.json`.
2. `TargetFramework` in `Directory.Build.props`.
3. Runtime-related versions in `Directory.Packages.props`.
4. `dotnet-ef` in `dotnet-tools.json`.

Then `mise install` and commit `mise.lock`. Check and verify with:

```bash
mise ls dotnet
dotnet --list-sdks
dotnet --version
dotnet list package --outdated
dotnet build && dotnet test
```

## Update Node

`mise.toml` asks for `22`, so a minor update is a lockfile change only:

```bash
mise lock --bump node
mise install
```

For a major update, edit only the version in the existing Node entry in `mise.toml`, preserving
the Corepack hook, then run `mise lock --bump node` and `mise install`. Review `engines.node` and
`@types/node` in `frontend/package.json`; raise the compatibility floor only when support changes.
Node 25 and newer no longer bundle Corepack, so an upgrade to that range also needs an explicit
Corepack installation. See [Corepack installation](https://github.com/nodejs/corepack#how-to-install).

## Update pnpm

Corepack manages pnpm from the `packageManager` field. From `frontend/`:

```bash
corepack up                    # Update within the current pnpm major
corepack use pnpm@<version>     # Select a specific version, including a new major
```

Choose one command. Both update `packageManager` and install dependencies; review and commit the
resulting `package.json` and any `pnpm-lock.yaml` changes. `pnpm self-update` refuses to run under
Corepack. See [Corepack's update commands](https://github.com/nodejs/corepack#corepack-up).

## Troubleshooting

| Symptom | Try |
| --- | --- |
| Frontend types do not match the API | From root: `dotnet build` / from `frontend/`: `pnpm gen:api` |
| Typecheck fails on a project reference | `pnpm exec nx sync` |
| Nx returns stale results | `pnpm exec nx reset` |
| `Cannot find module '@absence-management/…'` | Add the dependency to the importing project |
| `dotnet ef` is unavailable | `dotnet tool restore` |
| `dotnet` or `node` is the wrong version | `mise doctor`, then `mise ls --current` |
| Entering the repository changes nothing | [Shell activation](#activate-mise-in-the-shell) missing |
| mise asks about an untrusted config | `mise trust`, after reading `mise.toml` |
| SDK is missing or mismatched | Compare `dotnet --list-sdks` with `sdk.version` in `global.json` |
| `pnpm` is not found after `mise install` | `mise exec -- corepack enable` |
| The IDE disagrees with the shell | Install the [Mise plugin](#jetbrains-ides) |
| Build fails on a warning | Fix it. Suppress only with a comment explaining why |
