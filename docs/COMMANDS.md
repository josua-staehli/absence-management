# Command Cheat Sheet

The commands that come up while working on this repository. Run commands from the repository root
unless noted otherwise.

See [README.md](../README.md) for the overview, [AGENTS.md](../AGENTS.md) for conventions, and
[BOOTSTRAP.md](BOOTSTRAP.md) for setup details.

## First-time setup

Requires [mise](https://mise.jdx.dev) and a running container runtime. mise installs the .NET SDK,
Node and pnpm, see [Tool versions](#tool-versions). Existing installations, such as a system-wide
.NET SDK, nvm or a global pnpm, can stay: inside the repository, mise puts its versions first on the
`PATH`.

### Install mise

**Windows:**

```powershell
winget install jdx.mise
```

winget installs mise for the current user, together with the Visual C++ runtime it needs if that is
missing. Open a new terminal afterwards, so that `mise` is on the `PATH`.

**macOS:**

```bash
brew install mise
```

**Linux:**

```bash
curl -fsSL https://mise.run | sh
```

The script installs to `~/.local/bin` and works on macOS without Homebrew, too. Debian, Ubuntu,
Fedora, Arch and Alpine package mise as well, see
[Installing mise](https://mise.jdx.dev/installing-mise.html).

### Activate mise in the shell

Activation is what switches the tool versions when the shell enters the repository. Add the line
for the shell to its profile, then open a new shell:

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

In PowerShell, these two lines create `$PROFILE` if it does not exist yet and append the line,
without touching what is already in there:

```powershell
if (-not (Test-Path $PROFILE)) { New-Item -ItemType File -Path $PROFILE -Force | Out-Null }
Add-Content $PROFILE '(&mise activate pwsh) | Out-String | Invoke-Expression'
```

Windows PowerShell 5.1 and PowerShell 7 (`winget install Microsoft.PowerShell`) both work, each with
its own `$PROFILE`. PowerShell 7 is recommended, because commands in this documentation chain with
`&&`, which 5.1 does not support. 5.1 also cannot tell mise about a `cd`, and mise warns about that
at every start. The versions are right anyway, since on Windows activation puts mise's shims on the
`PATH`; `setx MISE_PWSH_CHPWD_WARNING 0` turns the warning off.

A shell without activation, a script or CI runs commands through mise instead, for example
`mise exec -- dotnet build`.

### Install the tools

From the repository root:

```bash
mise trust            # Confirm this repository's mise.toml, once per clone
mise install          # The .NET SDK, Node and pnpm, as pinned in mise.lock
dotnet tool restore   # Restore dotnet-ef
```

Verify:

```bash
mise doctor        # Activation and configuration
mise ls --current  # The active versions, and the file each one comes from
dotnet --version
node --version
pnpm --version
docker version
```

Aspire installs frontend dependencies automatically. For frontend-only work, run this from
`frontend/`:

```bash
pnpm install
pnpm exec playwright install   # Only required for pnpm e2e
```

## Tool versions

Every version has one place. mise reads the .NET SDK from `global.json`, Node and pnpm are declared
in `mise.toml`, and `mise.lock` records what these requests resolved to:

| Tool     | Version lives in              | How to update               |
| -------- | ----------------------------- | --------------------------- |
| .NET SDK | `global.json` → `sdk.version` | [Update .NET](#update-net)  |
| Node     | `mise.toml` → `node`          | [Update Node](#update-node) |
| pnpm     | `mise.toml` → `pnpm`          | [Update pnpm](#update-pnpm) |

`mise.toml`, `mise.lock` and `global.json` are committed. mise writes the lockfile itself, it is
never edited by hand.

```bash
mise ls --current                 # The active versions, and the file each one comes from
mise outdated                     # Newer versions the requests allow (--bump: beyond them)
mise upgrade                      # Install those, and record them in mise.lock
mise install --locked             # Exactly what mise.lock records, as CI installs
mise lock                         # Refresh mise.lock without installing anything
mise exec -- dotnet --list-sdks   # One command in mise's environment, without activation
mise doctor                       # Diagnose activation and configuration
```

mise updates itself through whatever installed it: `winget upgrade jdx.mise`, `brew upgrade mise`,
or `mise self-update` after the script install.

Personal deviations belong in `mise.local.toml` next to `mise.toml`. It is git-ignored, together
with the lockfile mise writes for it:

```toml
[tools]
node = '24'
```

## IDE integration

### JetBrains IDEs

Install the [Mise plugin](https://plugins.jetbrains.com/plugin/24904-mise), which needs Rider or
WebStorm 2026.1 or newer: <kbd>Settings</kbd> → <kbd>Plugins</kbd> → <kbd>Marketplace</kbd> →
`Mise`. It hands the IDE the tools of `mise.toml`, the same ones the shell gets.

Its project settings are committed, in `.idea/.idea.AbsenceManagement/.idea/mise.xml` for the
solution and in `frontend/.idea/mise.xml` for the WebStorm project. Under <kbd>Settings</kbd> →
<kbd>Tools</kbd> → <kbd>Mise Settings</kbd> that is:

| Setting                                 | State          | Effect                                                        |
| --------------------------------------- | -------------- | ------------------------------------------------------------- |
| Use environment variables from mise     | On, default    | The master switch                                             |
| Use in run configurations               | On, default    | The AppHost and the tests get mise's `PATH` and `DOTNET_ROOT` |
| Use in Nx commands                      | On, default    | Nx Console runs with mise's Node and pnpm                     |
| Use in all other command line execution | On, committed  | The terminal and external tools see the same versions         |

The plugin also sets the Node.js interpreter from mise, and in the WebStorm project the package
manager (<kbd>Settings</kbd> → <kbd>Languages & Frameworks</kbd> → <kbd>Node.js</kbd>).

The plugin does not set Rider's .NET toolset. Rider resolves that by itself, before any run
configuration starts, and finds a system-wide SDK or none rather than mise's. So point
<kbd>Settings</kbd> → <kbd>Build, Execution, Deployment</kbd> → <kbd>Toolset and Build</kbd> →
*.NET CLI executable path* at mise's shared SDK directory, once per machine:

| OS            | .NET CLI executable path                     |
| ------------- | -------------------------------------------- |
| Windows       | `%LOCALAPPDATA%\mise\dotnet-root\dotnet.exe` |
| macOS, Linux  | `~/.local/share/mise/dotnet-root/dotnet`     |

Every SDK mise installs lives there, so the setting survives SDK updates, and `global.json` still
selects the version.

### VS Code

`frontend/.vscode/extensions.json` recommends the mise extension, `hverlin.mise-vscode`. It loads
the environment of `mise.toml` into VS Code, and with `mise.configureExtensionsAutomatically` it
also points other extensions at the tools mise installed.

### Other editors

An editor without a mise integration finds the tools through mise's shims directory: on Windows,
add `%LOCALAPPDATA%\mise\shims` to the user `Path` environment variable; on macOS and Linux, add
`eval "$(mise activate zsh --shims)"` to `~/.zprofile`, or `eval "$(mise activate bash --shims)"`
to `~/.bash_profile`. Restart the editor afterwards.

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

The SDK version lives in `global.json` only, and mise installs exactly that version. A patch or
feature-band update within .NET 10 is therefore one edit, for example:

```json
{ "sdk": { "rollForward": "latestPatch", "version": "10.0.402" } }
```

```bash
mise install   # Installs the new SDK and records it in mise.lock
```

The version has to be a published, complete SDK version, see the
[.NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0). mise ignores `rollForward`,
.NET applies it: `latestPatch` also accepts a higher patch of the same feature band, if one is
installed, for example by another project that uses mise. `disable` would insist on the exact
version.

For a major update, change these together:

1. `sdk.version` in `global.json`.
2. `TargetFramework` in `Directory.Build.props`.
3. Runtime-related versions in `Directory.Packages.props`.
4. `dotnet-ef` in `dotnet-tools.json`.

Then `mise install`, and commit `mise.lock` with the rest. Check and verify with:

```bash
mise ls dotnet
dotnet --list-sdks
dotnet --version
dotnet list package --outdated
dotnet build && dotnet test
```

## Update Node

`mise.toml` requests `26`, so moving to the newest 26.x only changes the lockfile:

```bash
mise upgrade node   # Installs the newest 26.x and records it in mise.lock
```

For a major update, change the request in `mise.toml` to the next LTS line and run `mise install`.
`@types/node` in `frontend/package.json` follows the major. `engines.node` is the range the
workspace supports and only moves when the workspace starts to need a newer Node.

## Update pnpm

pnpm is a mise tool like Node; neither Corepack nor pnpm itself manage its version here:

```bash
mise upgrade pnpm   # Installs the newest 12.x and records it in mise.lock
```

For a major update, change the request in `mise.toml`, run `mise install`, then `pnpm install` from
`frontend/`, and commit `mise.toml`, `mise.lock` and `pnpm-lock.yaml` together. `pnpm self-update`
is not the way, which is also why `pnpm-workspace.yaml` turns off pnpm's update notice.

## Troubleshooting

| Symptom | Try |
| --- | --- |
| Frontend types do not match the API | From root: `dotnet build` / from `frontend/`: `pnpm gen:api` |
| Typecheck fails on a project reference | `pnpm exec nx sync` |
| Nx returns stale results | `pnpm exec nx reset` |
| `Cannot find module '@absence-management/…'` | Add the dependency to the importing project |
| `dotnet ef` is unavailable | `dotnet tool restore` |
| `dotnet`, `node` or `pnpm` has the wrong version | `mise doctor`, then `mise ls --current` |
| Entering the repository changes nothing | [Shell activation](#activate-mise-in-the-shell) is missing |
| mise asks whether to trust `mise.toml` | `mise trust`, after reading the file |
| `chpwd functionality requires PowerShell version 7` | Harmless in Windows PowerShell 5.1: `setx MISE_PWSH_CHPWD_WARNING 0`, or use PowerShell 7 |
| `The token '&&' is not a valid statement separator` | Windows PowerShell 5.1: use PowerShell 7, or run the commands one by one |
| PowerShell does not load `$PROFILE`: *running scripts is disabled* | `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` |
| pnpm warns *The store at … is not used* | Harmless: the repository is on another drive than the user profile, so pnpm keeps its store at that drive's root |
| `mise install --locked` misses a lockfile entry | `mise lock`, then commit `mise.lock` |
| SDK is missing or mismatched | Compare `mise ls dotnet` with `sdk.version` in `global.json` |
| The IDE uses other versions than the shell | Install the [mise plugin or extension](#ide-integration) |
| Build fails on a warning | Fix it. Suppress only with a comment explaining why |
