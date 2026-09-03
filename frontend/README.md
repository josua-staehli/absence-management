# Frontend

The frontend of the absence management application: one [Nx](https://nx.dev) workspace that builds
two React applications, `web` for employees and `admin` for approvers, out of one set of libraries.

It is a **package-based** workspace. Every project is an ordinary npm package with its own
`package.json`, pnpm workspaces link them, and Nx reads the graph from those files. There is no
`project.json` anywhere — tags and targets live under the `nx` key of each `package.json`.

## Layout

```text
apps/web/          the employee application: create and edit requests
apps/admin/        the approver application: approve and reject requests, list employees
apps/*-e2e/        one Playwright project per application
packages/absences/    the absences area, split into feature and data-access
packages/employees/   the employees area, same split
packages/shared/      api-client, i18n and ui, usable by every area
openapi/           the OpenAPI document written by `dotnet build`, checked in
```

Two axes of tags decide who may import whom, enforced by Nx's module-boundary rule during
`pnpm lint`: `scope:` is the feature area (`absences`, `employees`, `shared`, `app`), `type:` is
the layer (`app`, `feature`, `data-access`, `ui`, `util`, `e2e`).

## Running it

The normal way is the Aspire AppHost from the repository root, which starts the database, the API
and both dev servers, and hands each application its port and its API address:

```bash
dotnet run --project aspire/AbsenceManagement.AppHost
```

For frontend-only work, with the API already running on its launch profile port 5180:

```bash
pnpm install
```

```bash
pnpm dev
```

`pnpm dev` serves `web` on port 4200, `pnpm dev:admin` serves `admin` on port 4201. Both proxy
`/api` to the backend, so there is no CORS setup.

## Checks

```bash
pnpm check
```

| Command             | Checks                                                         |
| ------------------- | -------------------------------------------------------------- |
| `pnpm typecheck`    | Every project compiles, including against the generated client |
| `pnpm lint`         | oxlint, including Nx's module-boundary rule                    |
| `pnpm format:check` | Verifies formatting with oxfmt                                 |
| `pnpm test`         | Vitest, per project                                            |
| `pnpm e2e`          | Playwright, one project per application, against the built app |
| `pnpm build`        | Regenerates the API client, then builds both applications      |

`pnpm check` runs the first three. Linting and formatting are oxlint and oxfmt. Nx 23.2's
experimental `@nx/oxlint` bridge exposes the project-graph-aware boundary rule to oxlint, so no
ESLint configuration or command is needed.

## The API client

`packages/shared/api-client/src/generated/` is generated from the OpenAPI document and checked in.
Never edit it — run `dotnet build` in the repository root, then:

```bash
pnpm gen:api
```

## Storybook

The presentational components of `packages/shared/ui` have a story per state, developed without an
API or an app around them:

```bash
pnpm storybook
```

## More

- [../README.md](../README.md) — the project overview
- [../docs/BOOTSTRAP.md](../docs/BOOTSTRAP.md) — how this workspace was built, step by step, and why
- [../AGENTS.md](../AGENTS.md) — the conventions of the repository
