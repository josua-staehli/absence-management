# Project Bootstrap

This document describes the steps that were taken to create the structure of this project. It
exists for documentation purposes only, the steps do not need to be repeated.

## Prerequisites

- A GitHub account
- Git installed locally (`git --version`)
- The .NET 10 SDK (see below)
- A container runtime for the PostgreSQL container (see below)
- The Aspire project templates, and optionally the Aspire CLI (see below)
- Node 22.12 or newer, with pnpm through Corepack (see below)

### Install the .NET 10 SDK

Either download the installer or use a package manager.

**Download:** get the SDK installer (not just the runtime) for the current operating system from
<https://dotnet.microsoft.com/download/dotnet/10.0> and run it.

**CLI:**

```bash
# Windows
winget install Microsoft.DotNet.SDK.10

# macOS
brew install --cask dotnet-sdk

# Linux
sudo apt-get install -y dotnet-sdk-10.0
```

Verify the installation, the version has to start with `10.`:

```bash
dotnet --version
dotnet --list-sdks
```

#### Update the SDK

The following commands update the SDK:

```bash
# Windows
winget upgrade Microsoft.DotNet.SDK.10

# macOS
brew upgrade --cask dotnet-sdk

# Linux
sudo apt-get update && sudo apt-get upgrade dotnet-sdk-10.0
```

When the SDK was installed with the downloaded installer, download and run the current installer
again instead.

### Container runtime

Aspire starts the database as a container, so a container runtime has to be running:

```bash
docker version
```

[Docker Desktop](https://www.docker.com/products/docker-desktop/) or Podman both work. Nothing else
has to be installed for the database, no local insetallation, no connection string by hand.

### Install the Aspire project templates

The Aspire project templates are a NuGet package, not part of the SDK. Check whether they are
already installed:

```bash
dotnet new list aspire
```

If the list is empty, install them:

```bash
dotnet new install Aspire.ProjectTemplates::13.5.0
```

The version pins the templates to the Aspire version used below (`Aspire.AppHost.Sdk` and the
`Aspire.Hosting.*` packages), so the generated projects match the packages. Leave off the `::version`
to get the newest one, and then use that version consistently in the steps below.

Optionally, the [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview) adds the
`aspire` command, which finds the AppHost through `aspire.config.json` instead of needing the project
path:

```bash
dotnet tool install --global Aspire.Cli --version 13.5.0
```

Everything in [Aspire configuration](#aspire-configuration) also works without it, with
`dotnet run --project aspire/AbsenceManagement.AppHost`.

### Node and pnpm

Node 22 or newer, Vite 8 requires at least 22.12. The Oxc tools ship as native binaries for
the same range. Check version:

```bash
node --version
```

pnpm comes with Node through Corepack, so it does not have to be installed separately. On Windows,
run the console with admin rights.

```bash
corepack enable
```

```bash
pnpm --version
```

## Preparations

### Create the Git repository on GitHub

On the personal GitHub page, add a new repository with the following settings:

| Setting         | Value                                                        |
| --------------- | ------------------------------------------------------------ |
| Repository name | `absence-management`                                         |
| Description     | A web application to create and manage absences of employees |
| Visibility      | Public (or Private)                                          |
| Add README      | Off                                                          |
| Add .gitignore  | No .gitignore                                                |
| Add license     | No license                                                   |

All initial files are added from the local clone, so the repository is created empty on purpose.

### Create the local Git repository

Clone the repository using a Git tool or the CLI:

```bash
git clone <URL>
cd absence-management
```

The default branch is `main`.

## Add basic files

### .gitignore

This file covers the .NET side and the entries that hold for the whole repository. It does not
cover the frontend: the Nx workspace brings a `.gitignore` of its own, and everything the
JavaScript toolchain generates is listed there, see [Frontend](#frontend).

```gitignore
## .NET
artifacts/
bin/
obj/
*.user

## Rider
# The .idea folder itself is committed, so the shared solution settings travel with the repository.
# This one file does not: Rider's Aspire plugin rewrites it whenever the AppHost starts, because
# the database container is published on a new port every run.
.idea/.idea.AbsenceManagement/.idea/dataSources.xml
```

Two `.gitignore` files rather than one, each next to the toolchain it describes: the root file is
about a solution that is built with `dotnet`, the frontend one about a workspace that is built with
`pnpm`. Neither has to know what the other generates, and the frontend file is the one the Nx
generators keep writing to.

The `.idea` folder is deliberately not ignored, so that the inspection profile, the encodings and
the VCS mapping are the same for everyone who opens the solution in Rider. `dataSources.xml` is the
exception: it is local state, it holds the database password in clear text, and Rider recreates it
on demand.

The agent entries are local settings and scratch worktrees of the coding agents. They sit here and
not in the frontend file, because a pattern in the root `.gitignore` applies to every folder below
it.

### .gitattributes

The team works on different operating systems, so the line endings are normalized and generated
files are kept out of diffs:

```gitattributes
# Normalize text files
* text=auto eol=lf

# Lockfiles and generated output: keep them out of diffs
package-lock.json  linguist-generated=true -diff
pnpm-lock.yaml     linguist-generated=true -diff
yarn.lock          linguist-generated=true -diff
```

`text=auto eol=lf` makes Git store every text file with LF line endings and check it out with LF as
well, independent of the local `core.autocrlf` setting. `linguist-generated=true` collapses the
lockfiles in diffs on GitHub and excludes them from the language statistics, `-diff` suppresses
their content in local diffs.

Adding these rules does not change files that are already committed. When line endings are
normalized later in an existing repository, the working tree has to be renormalized once:

```bash
git add --renormalize .
```

### Documentation files

Add the entry point of the documentation:

- `README.md` — short project overview and how to get started
- `AGENTS.md` — the conventions of the repository for coding agents

`AGENTS.md` is read automatically by the common coding agents. It is kept short and only holds what
cannot be seen from the code itself: the folder layout, the build and test commands, and the
settings that the central files enforce. It has to be updated when one of these changes.

## Set up .NET Solution

### Create Solution

Create an empty Solution with an IDE like Rider, or with the CLI:

```bash
dotnet new sln --name AbsenceManagement
```

Make sure the Solution file is created in the `.slnx` format. The .NET 10 SDK uses `slnx` by
default, so `AbsenceManagement.slnx` is created without further options. When creating the solution
from an IDE, or with an older SDK, select the format explicitly:

```bash
dotnet new sln --format slnx --name AbsenceManagement
```

### Add common files

#### global.json:

Add a `global.json` file to pin the SDK version, so that every developer and the build server use
the same SDK:

```bash
dotnet new globaljson --sdk-version 10.0.0 --roll-forward latestFeature
```

This creates the following file in the repository root:

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.0"
  }
}
```

The `latestFeature` policy selects the highest installed 10.0 SDK, so patch and feature band
updates are picked up automatically, while .NET 11 is not used by accident. Add `--force` to the
command to overwrite an existing `global.json`.

#### Directory.Build.props:

Add a `Directory.Build.props` file to define MSBuild properties for all projects at once. MSBuild
imports it automatically into every project below the repository root, so the settings do not have
to be repeated in each `.csproj`:

```bash
dotnet new buildprops --use-artifacts
```

The template adds the artifacts output layout. The compiler and code style settings are added by
hand, so that the resulting file looks as follows:

```xml
<Project>
  <!-- See https://aka.ms/dotnet/msbuild/customize for more details on customizing your build -->

  <PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
  </PropertyGroup>

  <!-- Settings shared by every .NET project in this repository. -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>

    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

| Property                  | Effect                                                                                                                                       |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `UseArtifactsOutput`      | Collects the build output of all projects in one `artifacts/` folder in the repository root instead of a `bin/` and `obj/` folder per project |
| `TargetFramework`         | The framework every project targets, declared once instead of in each `.csproj`                                                               |
| `LangVersion`             | Uses the newest C# version the SDK supports                                                                                                   |
| `Nullable`                | Enables nullable reference types, so that possible `null` values are reported by the compiler                                                 |
| `ImplicitUsings`          | Adds common `using` directives implicitly                                                                                                    |
| `TreatWarningsAsErrors`   | Makes the build fail on warnings, so that they cannot pile up                                                                                |
| `EnforceCodeStyleInBuild` | Reports the code style rules of the `.editorconfig` during the build instead of only in the IDE                                             |

Make sure the `artifacts/` folder is excluded by the `.gitignore`.

#### Directory.Packages.props:

Add a `Directory.Packages.props` file to manage the NuGet package versions centrally:

```bash
dotnet new packagesprops
```

The template only enables central package management. Transitive pinning is added by hand, so that
the resulting file looks as follows:

```xml
<Project>
  <PropertyGroup>
    <!-- Enable central package management, https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
</Project>
```

With central package management, every package version is declared once in the `ItemGroup` of this
file, and the projects reference the package without a version:

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Serilog" Version="4.3.0" />

<!-- src/Some.Project/Some.Project.csproj -->
<PackageReference Include="Serilog" />
```

`CentralPackageTransitivePinningEnabled` extends this to transitive dependencies: a version declared
here also applies to packages that are only pulled in indirectly, without referencing the package in
a project.

The usual reason to reach for it is a security advisory against a package that nothing references
directly: pinning the patched version here fixes it without waiting for the package that pulls it
in to ship an update of its own.

The `ItemGroup` stays empty until the first project is added.

#### .editorconfig:

Add an `.editorconfig` file to define the formatting and code style rules for the whole repository.
IDEs apply the rules directly, so the code is formatted the same way regardless of who edits it and
which editor is used:

```bash
dotnet new editorconfig
```

The template generates almost 400 lines, which mostly repeat the defaults of the compiler. A
reduced version is enough:

```editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true
max_line_length = 100

# XML, project, config and data files
[*.{xml,csproj,proj,slnx,props,targets,config,nuspec,json,yml,yaml}]
indent_size = 2

[*.csproj]
ij_xml_space_inside_empty_tag = true

[*.slnx]
ij_xml_space_inside_empty_tag = true
max_line_length = off
ij_xml_attribute_wrap = off

[*.cs]
resharper_csharp_braces_for_ifelse = not_required
resharper_csharp_braces_redundant = false
resharper_arguments_skip_single = true
resharper_redundant_argument_name_specification_highlighting = none
```

`root = true` stops the lookup in the parent folders, and `end_of_line = lf` matches the
normalization of the `.gitattributes`.

The keys with the `resharper_` prefix are read by ReSharper and Rider. They are part of the
`.editorconfig` on purpose: a rule that lives in a personal IDE profile applies to one machine,
a rule that lives here applies to everybody who opens the repository.

Code style rules are checked during the build as well, because `EnforceCodeStyleInBuild` is enabled
in the `Directory.Build.props`. They only show up there when they carry a severity of `warning` or
`error`, and such a rule then fails the build, since `TreatWarningsAsErrors` is enabled too.

#### dotnet-tools.json:

Add a tool manifest so that the command line tools of the project are pinned in the repository
instead of being installed per machine:

```bash
dotnet new tool-manifest
```

The template writes the manifest to `.config/dotnet-tools.json`. Here it sits in the repository
root as `dotnet-tools.json` instead - the CLI looks for both names, and the rest of the shared
configuration is at the root too.

Install the tools into the manifest:

```bash
dotnet tool install dotnet-ef --version 10.0.11
```

`dotnet-ef` is the Entity Framework Core CLI, used to create and apply the database migrations. The
version is given explicitly, because the tool has to match the EF Core packages of the projects.
The version therefore has to be kept in sync with the `Microsoft.EntityFrameworkCore.*` versions in
the `Directory.Packages.props`.

After a fresh clone the tools have to be downloaded once:

```bash
dotnet tool restore
```

## Create .NET project structure

### Common projects

Four shared projects that every module builds on. They contain no business logic, only the building
blocks the modules reuse:

| Project                 | Content                                                                 |
| ----------------------- | ----------------------------------------------------------------------- |
| `Common.Domain`         | Base types of the domain model, e.g. entities, aggregate roots, results  |
| `Common.Application`    | Abstractions of the use case layer, e.g. handlers and the unit of work   |
| `Common.Infrastructure` | Shared persistence, e.g. `DbContext` conventions and configurations      |
| `Common.Api`            | Shared web concerns, e.g. endpoint helpers and error handling            |

```bash
dotnet new classlib -o src/Common/Common.Domain
```

```bash
dotnet new classlib -o src/Common/Common.Application
```

```bash
dotnet new classlib -o src/Common/Common.Infrastructure
```

```bash
dotnet new classlib -o src/Common/Common.Api
```

The template puts a `Class1.cs` placeholder in each project, which can be deleted right away. In
`.csproj` there are build props like `<TargetFramework>`, `<ImplicitUsings>` or `<Nullable>`, which
can be deleted too, because they come from the `Directory.Build.props`.

Add the projects to the solution:

```bash
dotnet sln AbsenceManagement.slnx add src/Common/Common.Domain src/Common/Common.Application src/Common/Common.Infrastructure src/Common/Common.Api
```

Wire the layering (inner layers know nothing about outer ones):

```bash
dotnet add src/Common/Common.Application reference src/Common/Common.Domain
```

```bash
dotnet add src/Common/Common.Infrastructure reference src/Common/Common.Application
```

```bash
dotnet add src/Common/Common.Api reference src/Common/Common.Application
```

Packages:

```bash
dotnet add src/Common/Common.Application package Microsoft.Extensions.DependencyInjection.Abstractions
```

```bash
dotnet add src/Common/Common.Infrastructure package Microsoft.EntityFrameworkCore
```

```bash
dotnet add src/Common/Common.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
```

Because central package management is enabled, `dotnet add package` writes the `PackageReference`
without a version into the `.csproj` and adds the matching `PackageVersion` to the
`Directory.Packages.props`.

`Common.Api` needs ASP.NET Core types but is not a web app itself, so it gets a framework reference
instead of a package. Add this to `Common.Api.csproj` by hand:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Build once to verify the projects and references resolve:

```bash
dotnet build
```

### Module: Employees

The first module. It repeats the four layers of the common projects, but holds the actual business
logic instead of the building blocks:

| Project                    | Content                                                               |
| -------------------------- | --------------------------------------------------------------------- |
| `Employees.Domain`         | The employee entities, value objects and the rules that apply to them  |
| `Employees.Application`    | The use cases, e.g. creating, updating and listing employees           |
| `Employees.Infrastructure` | The `DbContext`, the entity configurations and the migrations          |
| `Employees.Api`            | The HTTP endpoints of the module                                       |

```bash
dotnet new classlib -o src/Modules/Employees/Employees.Domain
```

```bash
dotnet new classlib -o src/Modules/Employees/Employees.Application
```

```bash
dotnet new classlib -o src/Modules/Employees/Employees.Infrastructure
```

```bash
dotnet new classlib -o src/Modules/Employees/Employees.Api
```

Delete the `Class1.cs` placeholders and build props like `<TargetFramework>`, `<ImplicitUsings>` or
`<Nullable>` in `.csproj`.

Add the projects to the solution:

```bash
dotnet sln AbsenceManagement.slnx add src/Modules/Employees/Employees.Domain src/Modules/Employees/Employees.Application src/Modules/Employees/Employees.Infrastructure src/Modules/Employees/Employees.Api
```

References: every layer references the layer below it inside the module and the matching layer of
the common projects, so the module never reaches around its own layering:

```bash
dotnet add src/Modules/Employees/Employees.Domain reference src/Common/Common.Domain
```

```bash
dotnet add src/Modules/Employees/Employees.Application reference src/Modules/Employees/Employees.Domain src/Common/Common.Application
```

```bash
dotnet add src/Modules/Employees/Employees.Infrastructure reference src/Modules/Employees/Employees.Application src/Common/Common.Infrastructure
```

```bash
dotnet add src/Modules/Employees/Employees.Api reference src/Modules/Employees/Employees.Infrastructure src/Common/Common.Api
```

The only extra package is the design time part of EF Core, the rest comes in through
`Common.Infrastructure`:

```bash
dotnet add src/Modules/Employees/Employees.Infrastructure package Microsoft.EntityFrameworkCore.Design
```

`Microsoft.EntityFrameworkCore.Design` provides the services that the `dotnet-ef` tool from the
`dotnet-tools.json` uses to create and apply the migrations. It is a development dependency, so
NuGet adds it with `PrivateAssets="all"`: it is not passed on to projects that reference
`Employees.Infrastructure` and it is not published with the application.

Build once to verify the projects and references resolve:

```bash
dotnet build
```

### Module: Absences

A second module. It repeats the four layers of the common projects, but holds the actual business
logic instead of the building blocks:

| Project                   | Content                                                             |
| ------------------------- | ------------------------------------------------------------------- |
| `Absences.Domain`         | The absence entities, value objects and the rules that apply to them |
| `Absences.Application`    | The use cases, e.g. requesting, approving and listing absences        |
| `Absences.Infrastructure` | The `DbContext`, the entity configurations and the migrations         |
| `Absences.Api`            | The HTTP endpoints of the module                                      |

```bash
dotnet new classlib -o src/Modules/Absences/Absences.Domain
```

```bash
dotnet new classlib -o src/Modules/Absences/Absences.Application
```

```bash
dotnet new classlib -o src/Modules/Absences/Absences.Infrastructure
```

```bash
dotnet new classlib -o src/Modules/Absences/Absences.Api
```

Delete the `Class1.cs` placeholders and build props like `<TargetFramework>`, `<ImplicitUsings>` or
`<Nullable>` in `.csproj`.

Add the projects to the solution:

```bash
dotnet sln AbsenceManagement.slnx add src/Modules/Absences/Absences.Domain src/Modules/Absences/Absences.Application src/Modules/Absences/Absences.Infrastructure src/Modules/Absences/Absences.Api
```

References: every layer references the layer below it inside the module and the matching layer of
the common projects, so the module never reaches around its own layering:

```bash
dotnet add src/Modules/Absences/Absences.Domain reference src/Common/Common.Domain
```

```bash
dotnet add src/Modules/Absences/Absences.Application reference src/Modules/Absences/Absences.Domain src/Common/Common.Application
```

```bash
dotnet add src/Modules/Absences/Absences.Infrastructure reference src/Modules/Absences/Absences.Application src/Common/Common.Infrastructure
```

```bash
dotnet add src/Modules/Absences/Absences.Api reference src/Modules/Absences/Absences.Infrastructure src/Common/Common.Api
```

The only extra package is the design time part of EF Core, the rest comes in through
`Common.Infrastructure`:

```bash
dotnet add src/Modules/Absences/Absences.Infrastructure package Microsoft.EntityFrameworkCore.Design
```

Build once to verify the projects and references resolve:

```bash
dotnet build
```

### Module boundary: Employees.Contracts

An absence request belongs to an employee, so the absences module has to be able to ask about one.
The four layers above give it no way to do that without breaking the boundary: a reference to
`Employees.Domain` would hand it the aggregate and its rules, a reference to
`Employees.Infrastructure` would hand it the table.

A fifth project of the employees module solves it. It holds nothing but the contract other modules
compile against, which is why it has no references and no packages of its own:

| Project               | Content                                                                 |
| --------------------- | ----------------------------------------------------------------------- |
| `Employees.Contracts` | What another module may know about an employee, and how it may ask for it |

```bash
dotnet new classlib -o src/Modules/Employees/Employees.Contracts
```

```bash
dotnet sln AbsenceManagement.slnx add src/Modules/Employees/Employees.Contracts
```

The implementation stays inside the owning module, so its infrastructure references the contract:

```bash
dotnet add src/Modules/Employees/Employees.Infrastructure reference src/Modules/Employees/Employees.Contracts
```

And the consumer references the contract - and nothing else of `Employees.*`:

```bash
dotnet add src/Modules/Absences/Absences.Application reference src/Modules/Employees/Employees.Contracts
```

That single reference is the whole coupling between the two modules.

### Tests

One test project per module in the `tests/` folder, next to `src/` rather than inside it, plus one
project for the rules that span all of them.

#### Module: Employees

```bash
dotnet new xunit -o tests/Modules/Employees.UnitTests
```

```bash
dotnet sln AbsenceManagement.slnx add tests/Modules/Employees.UnitTests
```

```bash
dotnet add tests/Modules/Employees.UnitTests reference src/Modules/Employees/Employees.Infrastructure
```

```bash
dotnet add tests/Modules/Employees.UnitTests package Microsoft.EntityFrameworkCore.Sqlite
```

#### Module: Absences

```bash
dotnet new xunit -o tests/Modules/Absences.UnitTests
```

```bash
dotnet sln AbsenceManagement.slnx add tests/Modules/Absences.UnitTests
```

```bash
dotnet add tests/Modules/Absences.UnitTests reference src/Modules/Absences/Absences.Infrastructure
```

```bash
dotnet add tests/Modules/Absences.UnitTests package Microsoft.EntityFrameworkCore.Sqlite
```

#### Architecture tests

The rules that hold across all modules belong to none of them, so they get their own project next
to `tests/Modules/`:

```bash
dotnet new xunit -o tests/Architecture/AbsenceManagement.ArchitectureTests
```

```bash
dotnet sln AbsenceManagement.slnx add tests/Architecture/AbsenceManagement.ArchitectureTests
```

```bash
dotnet add tests/Architecture/AbsenceManagement.ArchitectureTests reference src/Host/AbsenceManagement.Api
```

`TngTech.ArchUnitNET` and `TngTech.ArchUnitNET.xUnitV3` go into `Directory.Packages.props` and are
referenced without a version, like every other package.

The host is the only reference, and that is the point: the host references every module, so
building this project puts every module assembly next to the test assembly. The rules find the
modules there rather than naming them.

#### Common changes

The template still scaffolds xUnit **v2** with inline versions. Edit the generated `.csproj`:
replace the `xunit` package with `xunit.v3`, drop `coverlet.collector`, remove every `Version="…"`
attribute (they now live in `Directory.Packages.props`), and add `<OutputType>Exe</OutputType>`
plus `<IsTestProject>true</IsTestProject>`. xUnit v3 test projects are executables.

One suppression is worth the line it costs:

```xml
<!--
  xUnit1051 asks for TestContext.Current.CancellationToken on every awaited call.
  These tests run against an in-memory database and finish in milliseconds, so the
  extra argument would only add noise.
-->
<NoWarn>$(NoWarn);xUnit1051</NoWarn>
```

Without it, `TreatWarningsAsErrors` turns the analyzer's suggestion into a failing build.

The tests come in two shapes. Domain tests need nothing at all, the aggregate has no dependencies.
The use case tests run the real handlers, repositories and EF Core mapping against an **in-memory
SQLite** database: real SQL, real mapping, no Docker and no PostgreSQL. That is what the
`Microsoft.EntityFrameworkCore.Sqlite` package above is for.

```bash
dotnet build
```

```bash
dotnet test
```

## Implementation

The added files and their purpose:

### Common projects

Two types out of these four show up in every layer below, so they are worth naming once: an `Error`
is a business failure as a value - a stable `Code`, a message for the UI, and a `Type` that decides
the status code - and a `Result` is what an operation that can fail returns instead of throwing. A
broken rule therefore travels from the aggregate to the endpoint as a return value, and exceptions
stay for what really is exceptional.

#### Common.Domain

| File                        | Purpose                                                                        |
| --------------------------- | ------------------------------------------------------------------------------ |
| `Primitives/Entity.cs`      | Base class for objects with an id. Equality by type and `Id`             |
| `Primitives/AggregateRoot.cs` | Marks the entry point of an aggregate. The only entity a repository loads    |
| `Results/Error.cs`          | A business error as a value (`Code`, `Message`, `Type`) instead of an exception |
| `Results/Result.cs`         | The outcome of an operation that can fail, with and without a return value      |

#### Common.Application

| File                          | Purpose                                                                            |
| ----------------------------- | ---------------------------------------------------------------------------------- |
| `Handlers/ICommandHandler.cs` | Interfaces for a use case that changes state, with and without a return value       |
| `Handlers/IQueryHandler.cs`   | Interface for a use case that only reads data                                       |
| `IUnitOfWork.cs`              | Transaction boundary of a use case, implemented by the module's `DbContext`          |
| `ApplicationRegistration.cs`  | Registers every handler of an assembly and the `TimeProvider` in the DI container    |

#### Common.Infrastructure

| File                                | Purpose                                                                          |
| ----------------------------------- | -------------------------------------------------------------------------------- |
| `Database/ModuleDbContext.cs`       | Base `DbContext` of a module, at the same time its `IUnitOfWork`                   |
| `Database/IDbInitializer.cs`        | Migrates and seeds the tables of one module. Every module brings its own           |
| `Database/DatabaseInitialization.cs` | Registers a module's initializer and runs all of them on startup                  |
| `Database/DesignTimeDbContextFactory.cs` | Base for a module's `dotnet ef` factory, so adding a migration needs no database |
| `InfrastructureRegistration.cs`     | Registers a module's `DbContext` with the shared connection and provider settings  |

#### Common.Api

| File                           | Purpose                                                                          |
| ------------------------------ | -------------------------------------------------------------------------------- |
| `ResultExtensions.cs`          | Turns a business `Error` into an HTTP response, as RFC 9457 problem details       |
| `ApiRegistration.cs`           | HTTP behavior every module shares: problem details and enums as strings          |
| `ModuleRegistration.cs`        | `AddModule<T>()`: the connection string, the handlers and the infrastructure of one module |
| `OpenApiDocumentGeneration.cs` | Placeholder connection strings for the build-time OpenAPI document generation      |

### Module: Employees

#### Employees.Domain

| File                 | Purpose                                                                     |
| -------------------- | --------------------------------------------------------------------------- |
| `Employee.cs`        | The employee aggregate root, created through a factory that validates it     |
| `EmployeeErrors.cs`  | The business errors of the aggregate, with their stable codes                |

#### Employees.Application

One file per use case: the request as a `record`, and the handler that answers it right next to it.
Nothing here knows about EF Core or HTTP, the layer only talks through the interfaces it declares
itself, which the infrastructure implements.

| File                      | Purpose                                                                        |
| ------------------------- | ------------------------------------------------------------------------------ |
| `IEmployeesUnitOfWork.cs` | The transaction boundary of the module, so a handler cannot save through another module's context |
| `IEmployeeRepository.cs`  | Write side access to the aggregate: add it, and ask whether an email is taken   |
| `IEmployeeQueries.cs`     | Read side, returns projections instead of aggregates                            |
| `EmployeeDto.cs`          | The read model of an employee as the UI shows it                                |
| `CreateEmployee.cs`       | Use case: create an employee, returns the new id                                |
| `GetEmployees.cs`         | Use case: list all employees                                                    |
| `GetEmployeeById.cs`      | Use case: a single employee, `NotFound` when there is none                      |

#### Employees.Infrastructure

The layer that implements what the application layer declares. It is the only place in the module
that knows EF Core.

| File                                                  | Purpose                                                        |
| ----------------------------------------------------- | -------------------------------------------------------------- |
| `Persistence/EmployeesDbContext.cs`                   | The context of the module, holds the `Employees` table and is its `IEmployeesUnitOfWork` |
| `Persistence/Configurations/EmployeeConfiguration.cs` | Maps the aggregate to the `employees` table                     |
| `Persistence/Repositories/EmployeeRepository.cs`      | The write side, adds an employee and checks whether an email is taken |
| `Persistence/Queries/EmployeeQueries.cs`              | The read side, projects into `EmployeeDto` inside the query     |
| `Persistence/EmployeesDbInitializer.cs`               | Applies the migrations of the module and seeds employees        |
| `Persistence/DesignTimeDbContextFactory.cs`           | Lets `dotnet ef` build the context without a database           |
| `Persistence/Migrations/`                             | The generated migrations of the module's tables                 |
| `EmployeeDirectory.cs`                                | Answers `IEmployeeDirectory` for other modules, with summaries instead of aggregates |
| `EmployeesInfrastructureRegistration.cs`              | Registers context, unit of work, repository, queries, directory and initializer in the DI container |

The files in `Persistence/Migrations/` are generated, not written by hand:

```bash
dotnet ef migrations add InitialCreate --project src/Modules/Employees/Employees.Infrastructure --output-dir Persistence/Migrations
```

#### Employees.Api

| File                            | Purpose                                                            |
| ------------------------------- | ------------------------------------------------------------------ |
| `Contracts/EmployeeContracts.cs` | The request and response models of the HTTP API                    |
| `Endpoints/EmployeeEndpoints.cs` | The routes under `/api/employees`, one per use case                |
| `EmployeesModule.cs`             | The seam between the host and the module: services and routes      |

The endpoints hold no logic. Each one resolves the handler of its use case, hands it the request
and turns the `Result` into a response with `ToHttpResult()` from `Common.Api`, a success becomes
`200`, `201` or `204`, an `Error` becomes problem details with the matching status code:

| `ErrorType`  | Status                      |
| ------------ | --------------------------- |
| `Validation` | `400 Bad Request`           |
| `NotFound`   | `404 Not Found`             |
| `Conflict`   | `409 Conflict`              |

| Route                        | Use case             | Success            |
| ---------------------------- | -------------------- | ------------------ |
| `GET /api/employees`         | `GetEmployeesQuery`  | `200` with the list |
| `GET /api/employees/{id}`    | `GetEmployeeByIdQuery` | `200`, or `404`  |
| `POST /api/employees`        | `CreateEmployeeCommand` | `201` with the new id |

#### Employees.Contracts

| File                    | Purpose                                                                   |
| ----------------------- | ------------------------------------------------------------------------- |
| `IEmployeeDirectory.cs` | The `EmployeeSummary` other modules see, and the two questions they may ask |

Two methods, and both exist because a use case of the absences module calls them: one employee by
id, to check that a request has a real employee behind it, and the names of a set of ids, for the
list. The set version is not convenience - it is what keeps a list of absences from turning into
one call across the boundary per row.

The summary carries an id, a display name and an email address. It is not the aggregate and never
becomes one, so the employees module can rename a property or add a rule without breaking anybody.

### Module: Absences

The module of the actual task. It has the same four layers as the employees module and one thing
they do not have: it depends on another module. Every business rule of absences is implemented
here.

#### Absences.Domain

| File                       | Purpose                                                                    |
| -------------------------- | -------------------------------------------------------------------------- |
| `AbsenceType.cs`           | Vacation, sickness, training, other                                         |
| `AbsenceStatus.cs`         | Open, approved, rejected                                                    |
| `DateRange.cs`             | Value object of an inclusive period, enforces that the start is not after the end |
| `AbsenceRequest.cs`        | The aggregate root: creating it, editing it, approving and rejecting it     |
| `AbsenceRequestErrors.cs`  | The business errors of the aggregate, with their stable codes               |

Where a rule lives follows from what it needs to see:

| Rule                                            | Enforced by                     | Why there                                          |
| ----------------------------------------------- | ------------------------------- | -------------------------------------------------- |
| start not after end                             | `DateRange.Create`              | A period that cannot exist invalidly is never checked twice |
| valid type, starts open                         | `AbsenceRequest.Create`         | Everything the aggregate can see by itself          |
| decide once, only while open                    | `AbsenceRequest.Approve/Reject` | The state machine of one request                    |
| no edit after a decision                        | `AbsenceRequest.Update`         | Same                                                |
| an employee has to exist                        | `CreateAbsenceRequestHandler`   | Only the employees module can answer it             |
| no overlap for the same employee                | `CreateAbsenceRequestHandler`, `UpdateAbsenceRequestHandler` | Spans all requests of an employee, which no single aggregate sees |

The employee is a plain `Guid` on the aggregate, not a reference to an `Employee`: employees are
another module, with another database.

#### Absences.Application

One file per use case: the request as a `record`, and the handler that answers it right next to it.

| File                            | Purpose                                                                  |
| ------------------------------- | ------------------------------------------------------------------------ |
| `IAbsencesUnitOfWork.cs`        | The transaction boundary of the module                                    |
| `IAbsenceRequestRepository.cs`  | Write side: the aggregate by id, the overlap check, and adding a request  |
| `IAbsenceRequestQueries.cs`     | Read side, returns rows instead of aggregates                             |
| `AbsenceRequestDto.cs`          | The row this module owns, and the read model the UI gets                  |
| `CreateAbsenceRequest.cs`       | Use case: create a request, returns the new id                            |
| `UpdateAbsenceRequest.cs`       | Use case: edit an open request                                            |
| `ApproveAbsenceRequest.cs`      | Use case: approve an open request                                         |
| `RejectAbsenceRequest.cs`       | Use case: reject an open request                                          |
| `GetAbsenceRequests.cs`         | Use case: list all requests, with the employee names                      |
| `GetAbsenceRequestById.cs`      | Use case: a single request, `NotFound` when there is none                 |

This is the layer that talks to the other module, and it does so through `IEmployeeDirectory`
only, it has no idea what implements it. Two places need it:

- `CreateAbsenceRequestHandler` asks whether the employee exists. The answer is a business
  error, not an exception, and reaches the frontend as `400` with the code
  `Absences.EmployeeUnknown`.
- `GetAbsenceRequestsHandler` asks for the names of the employees in the list. Before the split
  this would have been a SQL join. Now it is one query per module and a lookup in memory, in **one**
  call for the whole list. The join happens in this layer instead of the database, because that is
  where the price of the boundary belongs.

An id whose employee no longer exists cannot be ruled out without a foreign key across the two
databases, so the read model shows a placeholder name and the list stays readable.

The employee is not part of `UpdateAbsenceRequestCommand`: it is the one value of a request that
does not change, so an edit cannot move an absence to somebody else.

#### Absences.Infrastructure

| File                                                        | Purpose                                                    |
| ----------------------------------------------------------- | ---------------------------------------------------------- |
| `Persistence/AbsencesDbContext.cs`                          | The context of the module, holds the `AbsenceRequests` table and is its `IAbsencesUnitOfWork` |
| `Persistence/Configurations/AbsenceRequestConfiguration.cs` | Maps the aggregate to the `absence_requests` table          |
| `Persistence/Repositories/AbsenceRequestRepository.cs`      | The write side, including the overlap check as a `SELECT`   |
| `Persistence/Queries/AbsenceRequestQueries.cs`              | The read side, projects into `AbsenceRequestRow` inside the query |
| `Persistence/AbsencesDbInitializer.cs`                      | Applies the migrations of the module                        |
| `Persistence/DesignTimeDbContextFactory.cs`                 | Lets `dotnet ef` build the context without a database       |
| `Persistence/Migrations/`                                   | The generated migrations of the module's tables             |
| `AbsencesInfrastructureRegistration.cs`                     | Registers context, unit of work, repository, queries and initializer in the DI container |

Three decisions are worth naming:

- The `DateRange` value object is mapped with `OwnsOne` into the columns `StartDate` and `EndDate`
  of the same table. It has no identity, so it gets no table of its own.
- The enums are stored as strings. That is readable in the database and survives a reordering of
  the enum values.
- There is **no** foreign key on `EmployeeId`, because the employee is in another database. The
  index stays: every write filters by employee for the overlap check.

Nothing is seeded here. An absence request needs an employee, and the sample employees belong to
the module that owns them.

The files in `Persistence/Migrations/` are generated, not written by hand:

```bash
dotnet ef migrations add InitialCreate --project src/Modules/Absences/Absences.Infrastructure --output-dir Persistence/Migrations
```

#### Absences.Api

| File                                    | Purpose                                                    |
| --------------------------------------- | ---------------------------------------------------------- |
| `Contracts/AbsenceRequestContracts.cs`  | The request and response models of the HTTP API             |
| `Endpoints/AbsenceRequestEndpoints.cs`  | The routes under `/api/absence-requests`, one per use case  |
| `AbsencesModule.cs`                     | The seam between the host and the module: services and routes |

| Route                                      | Use case                       | Success               |
| ------------------------------------------ | ------------------------------ | --------------------- |
| `GET /api/absence-requests`                | `GetAbsenceRequestsQuery`      | `200` with the list    |
| `GET /api/absence-requests/{id}`           | `GetAbsenceRequestByIdQuery`   | `200`, or `404`        |
| `POST /api/absence-requests`               | `CreateAbsenceRequestCommand`  | `201` with the new id  |
| `PUT /api/absence-requests/{id}`           | `UpdateAbsenceRequestCommand`  | `204`                  |
| `POST /api/absence-requests/{id}/approve`  | `ApproveAbsenceRequestCommand` | `204`                  |
| `POST /api/absence-requests/{id}/reject`   | `RejectAbsenceRequestCommand`  | `204`                  |

The failures follow the same mapping as every other module: a broken rule of the request itself
becomes `400`, an unknown id `404`, and a rule about other requests - an overlap, or a decision
that has already been made - `409`.

The host learns about the module in two lines of `Program.cs`, `builder.AddAbsencesModule()` and
`app.MapAbsencesModule()`, plus its connection string name in the placeholder list. It never sees a
handler, the `DbContext` or an endpoint. That the employees module has to be registered as well is
not visible here either: the absences module asks the container for `IEmployeeDirectory`, and the
container has it because the employees module registered it.

### Tests

The test projects were created further above, this is what went into them. The tests come in two
shapes, and the split follows what a test actually needs.

Domain tests construct the aggregate and nothing else: no fixture, no database, no test doubles.
The aggregate has no dependencies, so there is nothing to substitute.

Use case tests run the real handlers against the real repository, the real queries and the real EF
Core mapping, on an in-memory SQLite database. They cover what a domain test cannot reach: the
rules that span more than one aggregate, and whether the values survive the trip through the
database. Nothing is mocked, so a broken mapping fails a test instead of passing it.

#### Reaching the internals

The handlers, the repository and the queries are `internal`, only the registration methods of a
module are visible to the rest of the solution. The tests drive those types directly, so both
projects hand their internals to the test assembly:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Employees.UnitTests" />
</ItemGroup>
```

This goes into `Employees.Application.csproj` and `Employees.Infrastructure.csproj`, and the same
line with `Absences.UnitTests` into the two projects of the absences module. The alternative,
making the types public just so a test can reach them, would widen the module's surface for no
other reason.

#### Module: Employees

| File                              | Purpose                                                                        |
| --------------------------------- | ------------------------------------------------------------------------------ |
| `Domain/EmployeeTests.cs`         | The rules of the aggregate: required values, the shape of the address, trimming |
| `UseCases/EmployeesFixture.cs`    | Builds the in-memory database with the real mapping, repository and queries     |
| `UseCases/CreateEmployeeTests.cs` | The creation use case, above all the uniqueness of the email address            |
| `UseCases/EmployeeQueryTests.cs`  | The read side: the list, its order, and the lookup by id                        |

The fixture opens a connection to `Filename=:memory:` and keeps it open, because the database lives
exactly as long as that connection does. `EnsureCreatedAsync()` then builds the tables from the same
entity configurations PostgreSQL gets, so the unique index on the email address is there as well.
Every test creates its own fixture with `await using`, which makes the tests independent of each
other without a single line of cleanup code.

One test pins down behavior instead of checking a rule: two addresses that differ only in casing are
two different addresses today, because the aggregate does not normalize the address and the unique
index compares case-sensitively. The test says so in its name and in a comment, so that adding the
normalization later has to be a deliberate change.

#### Module: Absences

| File                                    | Purpose                                                              |
| --------------------------------------- | -------------------------------------------------------------------- |
| `Domain/AbsenceRequestTests.cs`         | The rules of the aggregate, without a database                  |
| `UseCases/FakeEmployeeDirectory.cs`     | Stands in for the whole employees module                             |
| `UseCases/AbsencesFixture.cs`           | Builds the in-memory database with the real mapping, repository and queries |
| `UseCases/AbsenceRequestUseCaseTests.cs` | Editing, deciding, and the read models                               |

The domain tests name the rule they cover in a comment.

The use case tests are where the boundary pays off. The absences module is exercised with the real
EF Core mapping and the real SQL, but the employees module is replaced by `FakeEmployeeDirectory` -
a small class that implements the contract and holds a dictionary. There is no employees database
in these tests, no seeded employee rows and no reference to `Employees.Infrastructure`: the only
thing to substitute is the contract, which is a far better description of what this module actually
depends on. A change inside the employees module cannot break them. A change to what the
two modules agreed on can, and should.

Two of the tests are about the boundary rather than a business rule:

- a request for an id no employee has is rejected
- a list whose employee ids the directory does not know still renders, with a placeholder name. No
  foreign key spans the two databases, so this is a state the module has to survive rather than one
  it can rule out.

#### Architecture tests

The two projects above test what the code does. This one tests how it is arranged, with
[ArchUnitNET](https://github.com/TNG/ArchUnitNET).

| File                       | Purpose                                                                     |
| -------------------------- | --------------------------------------------------------------------------- |
| `SolutionArchitecture.cs`  | Finds the assemblies and the modules, and holds the naming convention as regular expressions |
| `LayerTests.cs`            | The layering inside a module, and that the domain stays free of frameworks   |
| `ModuleBoundaryTests.cs`   | That a module reaches another one only through its contracts                 |
| `ConventionTests.cs`       | Where handlers, repositories, queries and endpoints live, and who may see them |

Most of the layering is already true without a test: the layers are separate projects, and the
compiler refuses a reference that would break them. What the tests add is the step before that.
Adding the `ProjectReference` that inverts a dependency, or that lets one module reach into another
one, is a change a reviewer has to notice today. Now it fails a test.

**No rule names a module.** Every project is called `<Owner>.<Layer>`, so the modules can be derived
from the assemblies that ended up next to the test assembly: whoever owns a `.Domain` assembly is a
module. The layer rules are regular expressions over that convention, and the boundary rule is a
theory over every ordered pair of modules. A third module is checked against the other two without
a line being added anywhere - it only has to be mounted in the host, which it has to be anyway. One
test asserts exactly that, so a module that never reached the host fails instead of being silently
exempt from every rule.

One rule does not use ArchUnitNET, and the reason is worth knowing before writing more of them:

```csharp
public void The_domain_references_nothing_but_the_base_class_library()
```

A domain project references no other project, so the only way a framework gets in is a
`PackageReference`. ArchUnitNET, however, only knows the types it was asked to load - a rule
phrased as "no type in the domain depends on a type under `Microsoft.*`" compares against an empty
set and passes without ever looking. The assembly references are the honest source for this one.
The same trap applies to any rule whose forbidden side is not part of the solution.

Every rule here was written twice: once as it stands, and once deliberately inverted, to see it
fail. A rule that has never failed is a rule nobody has checked.

## Aspire configuration

Until now the solution is a set of class libraries: they compile, but nothing runs them and there is
no database. .NET Aspire closes that gap for local development. One command starts everything the
application needs: the PostgreSQL container, the web host with all modules, and later the two
frontend dev servers. It wires the connection strings between them and shows the whole system in a
dashboard with logs, traces and metrics.

Three pieces are added here:

| Piece                                 | Where                       | Role                                                                   |
| ------------------------------------- | --------------------------- | ---------------------------------------------------------------------- |
| `AbsenceManagement.ServiceDefaults`   | `src/Host/`                 | Telemetry, health checks, service discovery and HTTP resilience, shared by every service |
| `AbsenceManagement.Api`               | `src/Host/`                 | The web application that hosts the modules, the only executable of `src/` |
| `AbsenceManagement.AppHost`           | `aspire/`                   | The orchestrator: it declares the resources (database, API, later the frontends) and starts them |

The AppHost sits outside `src/` on purpose: it is not part of the application that gets deployed, it
only describes how the parts are run together during development. It is also the only project that
knows every other one.

### Project structure

The state at the end of this section:

```text
absence-management/
├─ aspire/
│  └─ AbsenceManagement.AppHost/
│     ├─ Properties/
│     │  └─ launchSettings.json          ports of the Aspire dashboard
│     ├─ AbsenceManagement.AppHost.csproj
│     ├─ AppHost.cs                      the resources and how they depend on each other
│     └─ appsettings.json
├─ src/
│  ├─ Common/                            (unchanged)
│  ├─ Modules/                           (unchanged)
│  └─ Host/
│     ├─ AbsenceManagement.Api/
│     │  ├─ Properties/
│     │  │  └─ launchSettings.json       contains port for running the API without Aspire
│     │  ├─ AbsenceManagement.Api.csproj
│     │  ├─ Program.cs                   service defaults, common API setup, one line per module
│     │  ├─ appsettings.json
│     │  └─ appsettings.Development.json
│     └─ AbsenceManagement.ServiceDefaults/
│        ├─ AbsenceManagement.ServiceDefaults.csproj
│        └─ Extensions.cs                generated by the template, left untouched
└─ aspire.config.json                    lets `aspire run` find the AppHost
```

### Service defaults

```bash
dotnet new aspire-servicedefaults -o src/Host/AbsenceManagement.ServiceDefaults
```

```bash
dotnet sln AbsenceManagement.slnx add src/Host/AbsenceManagement.ServiceDefaults
```

The template writes `Extensions.cs` and the `.csproj`. `Extensions.cs` is left exactly as it is, it
is Aspire's standard cross-cutting setup, and editing it would only make it harder to compare with
the template on the next update. The `.csproj` needs the two adjustments described below. What
`Extensions.cs` provides:

| Method                     | What it does                                                                        |
| -------------------------- | ----------------------------------------------------------------------------------- |
| `AddServiceDefaults()`     | Calls the three below and adds service discovery plus a standard resilience handler for every `HttpClient` |
| `ConfigureOpenTelemetry()` | Logs, metrics and traces for ASP.NET Core, `HttpClient` and the runtime, exported over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, which is exactly what the AppHost injects |
| `AddDefaultHealthChecks()` | Registers the `self` check that reports whether the process is responsive            |
| `MapDefaultEndpoints()`    | Maps `/health` (all checks) and `/alive` (only the checks tagged `live`)              |

Two details are worth knowing, because both show up later:

- `MapDefaultEndpoints()` only maps the endpoints in the development environment. Exposing health
  data publicly leaks information about the system, so the template guards it. The AppHost's
  `WithHttpHealthCheck("/health")` therefore works while developing and would need a different
  endpoint in production.
- The delivered `.csproj` sets `IsAspireSharedProject`, which is what allows `AddServiceDefaults()`
  to be called from the host project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsAspireSharedProject>true</IsAspireSharedProject>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App"/>

    <PackageReference Include="Microsoft.Extensions.Http.Resilience"/>
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery"/>
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol"/>
    <PackageReference Include="OpenTelemetry.Extensions.Hosting"/>
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore"/>
    <PackageReference Include="OpenTelemetry.Instrumentation.Http"/>
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime"/>
  </ItemGroup>

</Project>
```

The template writes the versions into the `PackageReference` elements. Central package management
does not allow that, so move each version into the `Directory.Packages.props` and delete the
`Version="…"` attributes here, the same edit as with the test projects:

```xml
<ItemGroup Label="ServiceDefaults (telemetry, health checks, resilience)">
  <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.6.0" />
  <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery" Version="10.6.0" />
  <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.3" />
  <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.15.3" />
  <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.2" />
  <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.15.1" />
  <PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="1.15.1" />
</ItemGroup>
```

`<TargetFramework>`, `<Nullable>` and `<ImplicitUsings>` come from the `Directory.Build.props` and
can be deleted from the generated `.csproj` as well.

### Host: AbsenceManagement.Api

The web application that hosts the modules:

```bash
dotnet new web -o src/Host/AbsenceManagement.Api
```

```bash
dotnet sln AbsenceManagement.slnx add src/Host/AbsenceManagement.Api
```

One project reference per module, plus the service defaults:

```bash
dotnet add src/Host/AbsenceManagement.Api reference src/Modules/Employees/Employees.Api src/Modules/Absences/Absences.Api src/Host/AbsenceManagement.ServiceDefaults
```

The host references only the `*.Api` project of each module. The other three layers come along as
transitive references, but nothing in `Program.cs` ever names them. The module exposes exactly two
methods, and that is the whole contract between host and module.

Packages:

```bash
dotnet add src/Host/AbsenceManagement.Api package Microsoft.AspNetCore.OpenApi
```

```bash
dotnet add src/Host/AbsenceManagement.Api package Scalar.AspNetCore
```

`Microsoft.AspNetCore.OpenApi` generates the OpenAPI document from the endpoints, `Scalar.AspNetCore`
renders it as an interactive API documentation at `/scalar/v1`, which replaces Swagger UI.

The resulting `AbsenceManagement.Api.csproj`, after deleting the `<TargetFramework>`, `<Nullable>`
and `<ImplicitUsings>` the template wrote (they come from the `Directory.Build.props`):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <!-- One reference per module, plus the shared web setup and the Aspire service defaults. -->
    <ProjectReference Include="..\..\Modules\Employees\Employees.Api\Employees.Api.csproj"/>
    <ProjectReference Include="..\..\Modules\Absences\Absences.Api\Absences.Api.csproj"/>
    <ProjectReference Include="..\AbsenceManagement.ServiceDefaults\AbsenceManagement.ServiceDefaults.csproj"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi"/>
    <PackageReference Include="Scalar.AspNetCore"/>
  </ItemGroup>

</Project>
```

[The generated API client](#the-generated-api-client) adds two more things to this file later: the
`Microsoft.Extensions.ApiDescription.Server` package and the properties that write the OpenAPI
document to disk on every build.

Delete the `Hello World` endpoint the template put into `Program.cs` and replace the file with:

```csharp
using Absences.Api;
using Common.Api;
using Common.Infrastructure.Database;
using Employees.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health checks and resilience (shared with every future service).
builder.AddServiceDefaults();

// Problem details and JSON settings that every module shares.
builder.Services.AddCommonApi();
builder.Services.AddOpenApi();

// Only has an effect while `dotnet build` generates the OpenAPI document: that starts the host
// without a database, and the modules below would otherwise refuse to register.
builder.AddPlaceholderConnectionStrings(
    EmployeesModule.ConnectionStringName,
    AbsencesModule.ConnectionStringName);

// --- Modules -------------------------------------------------------------
// One line per module. Each module reads its own connection string and registers its own use
// cases, repositories and database initializer. The order is irrelevant: modules never call each
// other during registration.
builder.AddEmployeesModule();
builder.AddAbsencesModule();
// -------------------------------------------------------------------------

var app = builder.Build();

app.UseCommonApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();                 // interactive API documentation at /scalar/v1
    await app.Services.InitializeModulesAsync(); // migrate and seed every module
}

app.MapDefaultEndpoints();

// --- Module routes -------------------------------------------------------
app.MapEmployeesModule();
app.MapAbsencesModule();
// -------------------------------------------------------------------------

app.Run();
```

Adding a module costs exactly three lines here, the connection string placeholder, the
`Add…Module()` and the `Map…Module()`. Nothing else in the host changes.

`InitializeModulesAsync()` runs every registered `IDbInitializer`, which applies the migrations and
seeds the development data. It is inside the `IsDevelopment()` block on purpose: applying migrations
automatically on startup is convenient locally and a bad idea in production, where a migration is a
deployment step of its own.

The `AddPlaceholderConnectionStrings` line belongs to
[The generated API client](#the-generated-api-client) and can be left out until then.

`appsettings.json` and the `appsettings.Development.json` next to it keep the logging defaults of
the template. No connection string is ever written into either of them, they come from Aspire at
runtime:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`Properties/launchSettings.json` — the template generates an `https` and an `http` profile with a
random port. The `https` profile is dropped and the `http` one pinned to port 5180, so that the API
has a predictable address when it is started without Aspire:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Started this way the API has no Aspire around it, so the connection strings have to come from the
environment, one per module:

```bash
$env:ConnectionStrings__employeedb = "Host=localhost;Port=5432;Database=employeedb;Username=postgres;Password=..."
$env:ConnectionStrings__absencedb  = "Host=localhost;Port=5432;Database=absencedb;Username=postgres;Password=..."
```

```bash
dotnet run --project src/Host/AbsenceManagement.Api
```

That is the exception, not the normal way to work, usually the AppHost below starts everything.

Build:

```bash
dotnet build
```

### The Aspire AppHost

```bash
dotnet new aspire-apphost -o aspire/AbsenceManagement.AppHost
```

```bash
dotnet sln AbsenceManagement.slnx add aspire/AbsenceManagement.AppHost
```

```bash
dotnet add aspire/AbsenceManagement.AppHost reference src/Host/AbsenceManagement.Api
```

```bash
dotnet add aspire/AbsenceManagement.AppHost package Aspire.Hosting.PostgreSQL
```

The template writes six files: `AppHost.cs`, the `.csproj`, `Properties/launchSettings.json`,
`appsettings.json`, `appsettings.Development.json` and an `aspire.config.json`. Two of them are not
kept as they are:

- `appsettings.Development.json` only repeats the two log levels of the base file and is deleted.
- `aspire.config.json` is written **next to the project** with a relative path
  (`"path": "AbsenceManagement.AppHost.csproj"`). It is moved to the repository root and its path
  adjusted, so that `aspire run` works from anywhere in the repository, see the end of this section.

In the `.csproj`, `<TargetFramework>`, `<Nullable>` and `<ImplicitUsings>` come from the
`Directory.Build.props` and are deleted, and the generated GUID in `<UserSecretsId>` is replaced with
a readable name. The result:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.5.0">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UserSecretsId>absence-management-apphost</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <!-- The AppHost only needs the host project; the modules come with it. -->
    <ProjectReference Include="..\..\src\Host\AbsenceManagement.Api\AbsenceManagement.Api.csproj"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.PostgreSQL"/>
  </ItemGroup>

  <!--
    Aspire 13.5 warns (ASPIRE010) when the AppHost does not use the Aspire CLI bundle. Using it
    would mean the orchestrator and the dashboard come from a globally installed `aspire` CLI
    instead of from NuGet, which would turn that CLI into a prerequisite for building this
    repository.
  -->
  <PropertyGroup>
    <NoWarn>$(NoWarn);ASPIRE010</NoWarn>
  </PropertyGroup>

</Project>
```

Without that `NoWarn`, the `TreatWarningsAsErrors` of the `Directory.Build.props` turns ASPIRE010
into a failing build - which is why the suppression is not optional here.

The version of the `Aspire.AppHost.Sdk` has to match the `Aspire.Hosting.*` packages, so both stay
on the same Aspire version:

```xml
<ItemGroup Label="Aspire (orchestration for local development)">
  <PackageVersion Include="Aspire.Hosting.PostgreSQL" Version="13.5.0" />
</ItemGroup>
```

The project reference does two things: it lets the AppHost start the API, and the Aspire SDK
generates a type for every referenced project in the `Projects` namespace, the project name with
dots replaced by underscores, so `AbsenceManagement.Api` becomes `Projects.AbsenceManagement_Api`.
That type is how `AppHost.cs` names the project without a path or a string.

`AppHost.cs` — the template generates an empty builder, the resources are added by hand:

```csharp
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL runs as a container. The data volume and the persistent lifetime keep the data
// between runs, pgAdmin gives a quick look into the database at development time.
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

// One database per module, named after the connection string that module asks for. Separate
// databases rather than one with two schemas: it is the cheapest way to make sure no query can
// ever join across a module boundary by accident.
var employeeDatabase = postgres.AddDatabase("employeedb");
var absenceDatabase = postgres.AddDatabase("absencedb");

builder.AddProject<AbsenceManagement_Api>("api")
    .WithReference(employeeDatabase)
    .WithReference(absenceDatabase)
    .WaitFor(employeeDatabase)
    .WaitFor(absenceDatabase)
    .WithHttpHealthCheck("/health")
    // A second link on the "api" resource, next to its own address. The Url is relative and gets
    // resolved against the "http" endpoint, whose port Aspire picks fresh on every run - which is
    // what makes the interactive documentation reachable without assembling the address by hand.
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar/v1", DisplayText = "API docs" });

builder.Build().Run();
```

What each call is responsible for:

| Call                                    | Effect                                                                                     |
| --------------------------------------- | ------------------------------------------------------------------------------------------ |
| `AddPostgres("postgres")`               | Starts a PostgreSQL container and generates the password for it                             |
| `WithImageTag("17")`                     | Pins the major version instead of following `latest`                                        |
| `WithDataVolume()`                       | Stores the data in a named Docker volume, so it survives a restart of the container         |
| `WithLifetime(ContainerLifetime.Persistent)` | Keeps the container running after `Ctrl+C`, which makes the next start fast              |
| `WithPgAdmin()`                          | Adds a pgAdmin container that is already connected to the database, linked from the dashboard |
| `AddDatabase("employeedb")`              | Creates the database in that container, the resource name is the connection string name     |
| `AddProject<AbsenceManagement_Api>("api")` | Starts the API project as a resource named `api`                                          |
| `WithReference(database)`                | Injects the connection string as `ConnectionStrings__<name>` into the API process            |
| `WaitFor(database)`                      | Starts the API only after the database is up and healthy                                    |
| `WithHttpHealthCheck("/health")`         | Marks the API as healthy only once that endpoint answers, which other resources can wait for |
| `WithUrlForEndpoint("http", …)`          | Adds a second link on the resource, here `/scalar/v1`, resolved against that endpoint's address |

The link between the two sides is the resource name and nothing else: `postgres.AddDatabase("employeedb")`
and `EmployeesModule.ConnectionStringName == "employeedb"`. `WithReference` turns that into the
environment variable `ConnectionStrings__employeedb`, which the module reads through
`builder.Configuration.GetConnectionString(…)`. No connection string is written into a file, and none
is checked into the repository.

`Properties/launchSettings.json` — the ports of the dashboard and its endpoints. The template
generates an `https` and an `http` profile with port numbers drawn at random. The `https` profile is
dropped, the ports of the `http` profile are kept as they are and committed, so that every developer
reaches the dashboard at the same address:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:15246",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19068",
        "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "http://localhost:20137",
        "ASPIRE_ALLOW_UNSECURED_TRANSPORT": "true"
      }
    }
  }
}
```

`applicationUrl` is the dashboard, the two `ASPIRE_*_URL` variables are the endpoints the resources
report their telemetry to. `ASPIRE_ALLOW_UNSECURED_TRANSPORT` is the only line added by hand: it
permits plain HTTP for those endpoints, which is what makes the `http` profile work without a
development certificate.

`appsettings.json` stays as the template writes it, it already turns the very talkative Aspire
process control (`Dcp`) down:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  }
}
```

Finally, the `aspire.config.json` generated inside the project folder is moved to the repository root
and its path made relative to it. That is what lets the Aspire CLI find the AppHost, so `aspire run`
works from anywhere in the repository:

```json
{
  "appHost": {
    "path": "aspire/AbsenceManagement.AppHost/AbsenceManagement.AppHost.csproj"
  }
}
```

```bash
dotnet build
```

### Run it

With Docker running:

```bash
dotnet run --project aspire/AbsenceManagement.AppHost
```

```bash
# with the Aspire CLI, from anywhere in the repository
aspire run
```

The first start pulls the PostgreSQL and pgAdmin images and takes a moment. Then the dashboard opens
at <http://localhost:15246> and lists the resources:

| Resource     | What it is                                                                   |
| ------------ | ----------------------------------------------------------------------------- |
| `postgres`   | The database container                                                        |
| `pgadmin`    | The database UI, already connected                                            |
| `employeedb` | The database of the Employees module                                          |
| `absencedb`  | The database of the Absences module                                           |
| `api`        | The web host with both modules, healthy once `/health` answers                |

Every resource shows its URL, its logs, its environment variables and its traces. Useful checks
after the first start:

| Check                | Where                                                                    |
| -------------------- | ------------------------------------------------------------------------ |
| The API is healthy   | `api` is green in the dashboard, `/health` returns `Healthy`             |
| The endpoints are there | the "API docs" link on `api`, which lists every module's routes        |
| The migrations ran   | the tables exist in pgAdmin, and the log of `api` shows the EF Core statements |
| Telemetry arrives    | the traces of a request show up under `api` in the dashboard              |

The API port is assigned by Aspire and changes between runs, the dashboard is the place to look it
up. That is intentional: nothing in the repository hardcodes it.

`Ctrl+C` stops the API, the PostgreSQL container keeps running because of
`ContainerLifetime.Persistent`, and its data survives in the volume. To start over with an empty
database, delete the volume while the container is stopped:

```bash
docker volume ls
```

```bash
docker volume rm <name of the volume>
```

The name follows the pattern `<repository>-postgres-data`. On the next start the container is
recreated, and the initializers migrate and seed it again.

## Frontend

The frontend is a **separate workspace** with its own `package.json`, its own tooling and its own
dependency graph. Nothing in `src/` knows it exists; the only connection is the HTTP contract and
the three resources the AppHost declares for it.

It is a **package-based** Nx workspace: every project is an ordinary npm package with its own
`package.json`, pnpm workspaces link them, and Nx reads the graph from those files. There is no
`project.json` anywhere — tags and targets live under an `nx` key inside each `package.json`.

The toolchain:

| Technology            | What it does                                                                |
| --------------------- | --------------------------------------------------------------------------- |
| Nx                    | The workspace: project graph, task caching, code generators                 |
| Vite 8 / Rolldown     | Dev server and bundler. Vite 8 uses Oxc as transformer and resolver and Rolldown as bundler, so the whole build path is already the Oxc stack |
| React 19              | The UI runtime                                                              |
| Mantine               | The UI library. CSS based, no runtime styling engine, no Emotion            |
| TanStack Query        | Server state: caching, invalidation, request de-duplication                 |
| react-i18next         | The English and German texts, with the keys checked by the compiler         |
| oxlint                | The linter for everything except the architecture rule                      |
| oxfmt                 | The formatter, in place of Prettier                                         |
| ESLint                | Kept for exactly one rule: `@nx/enforce-module-boundaries`                  |
| Vitest                | Unit tests, per project                                                     |
| Playwright            | End-to-end tests, one project per application                               |
| `@hey-api/openapi-ts` | Generates the typed API client from the OpenAPI document of the backend     |

Two applications are built from it, `web` for employees and `admin` for approvers, out of one set
of libraries.

The backend does not have to be touched for the steps up to and including the boundaries. The
OpenAPI document the API client is generated from is not written yet either - the build that
produces it is wired up in [The generated API client](#the-generated-api-client) below, and that
section is where `dotnet build` becomes a prerequisite of `pnpm gen:api`.

### Create the Nx workspace

Create the workspace, from the repository root:

```bash
npx create-nx-workspace@latest frontend --appName=web --preset=react-monorepo --workspaceType=package-based --formatter=none --bundler=vite --unitTestRunner=vitest --e2eTestRunner=playwright --nxCloud=skip --packageManager=pnpm --skipGit --aiAgents=none --analytics=false --interactive=false
```

`--interactive=false` means no prompt is shown, so every answer has to be on the command line — the
flags above are those answers. `--workspaceType=package-based` is the one that decides the layout:
each project is configured in its own `package.json` and no `project.json` is written anywhere.
`--formatter=none` keeps Prettier out, oxfmt takes its place; `--aiAgents=none` keeps the agent
folders out; `--skipGit` leaves the repository that already exists alone.

What the command writes:

| Path                                  | What it is                                                                                        |
| ------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `apps/web`                            | The application named by `--appName`: React 19, `vite.config.mts` with the Vitest block inside it  |
| `apps/web-e2e`                        | Its Playwright project, with an `implicitDependencies` entry back to the application               |
| `nx.json`                             | The plugins that infer the targets: `@nx/js/typescript`, `@nx/eslint`, `@nx/vite`, `@nx/vitest`, `@nx/playwright` |
| `package.json`                        | Named `@frontend/source` — the npm scope is the name of the workspace folder. React 19, Vite 8, Vitest 4, TypeScript 6, Playwright and the full ESLint stack |
| `pnpm-workspace.yaml`                 | The `packages:` glob `apps/*`, plus `autoInstallPeers` and `allowBuilds`                           |
| `tsconfig.base.json`, `tsconfig.json` | The shared compiler options, and the solution-style project references with one entry per project  |
| `vitest.config.ts`                    | Collects the per-project Vitest configurations into one run                                        |
| `.editorconfig`, `eslint.config.mjs`, `.gitignore`, `README.md`, `.vscode/` | The workspace-wide files                                     |

Everything from here on runs inside `frontend/`.

Generate the second application:

```bash
pnpm exec nx g @nx/react:application apps/admin --bundler=vite --unitTestRunner=vitest --e2eTestRunner=playwright --style=css --routing=true --no-interactive
```

It produces two projects, the application and the Playwright project `apps/admin-e2e` next to it,
the same pair the creation command produced for `web`.

Generate the libraries. The folder names encode two things, the **scope** (which feature area) and
the **type** (which layer). `--name` gives each library the package name which is used to import it.

```bash
pnpm exec nx g @nx/js:library packages/shared/api-client --name=@absence-management/shared-api-client --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/shared/i18n --name=@absence-management/shared-i18n --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/shared/ui --name=@absence-management/shared-ui --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/employees/data-access --name=@absence-management/employees-data-access --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/employees/feature --name=@absence-management/employees-feature --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/absences/data-access --name=@absence-management/absences-data-access --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

```bash
pnpm exec nx g @nx/react:library packages/absences/feature --name=@absence-management/absences-feature --bundler=none --unitTestRunner=none --linter=none --component=false --style=none --no-interactive
```

In a package-based workspace the generators write the project configuration into each project's
`package.json`, which is the point of the whole layout. Every generator also appends its folder to
the `packages:` globs of `pnpm-workspace.yaml` and its project to the `references` of the root
`tsconfig.json`.

Runtime dependencies. `-w` puts them into the **root** `package.json`, next to the React version
the creation command added: one version of Mantine for the whole workspace ("single version
policy"), which is what you want in a monorepo that ships a single bundle:

```bash
pnpm add -w @mantine/core @mantine/hooks @mantine/dates @mantine/form dayjs @tanstack/react-query i18next react-i18next
```

`@mantine/dates` covers the date range of an absence request and needs `dayjs`; `@mantine/form`
covers the validation of the request form. `i18next` and `react-i18next` are installed here as well,
because the error interceptor of the API client already needs them, see
[Texts and languages](#texts-and-languages).

Development dependencies — Mantine's PostCSS preset, the Oxc tools, the parser for the one ESLint
rule that stays, and the client generator. `eslint` and `@nx/eslint-plugin` are missing from this
list because the creation command has already installed them:

```bash
pnpm add -w -D postcss postcss-preset-mantine postcss-simple-vars oxlint oxfmt @typescript-eslint/parser @hey-api/openapi-ts
```

Then a set of corrections to what the creation command and the generators wrote:

1. **Rename the npm scope of the root package and the four projects under `apps/`.** The scope is
   the name of the workspace folder, so everything that was not generated with a `--name` is called
   `@frontend/…`: the root package `@frontend/source`, `@frontend/web`, `@frontend/admin` and the
   two e2e projects. Replace the scope in the `name` of those five `package.json` files, and follow
   it in the two places that repeat one of those names — the `implicitDependencies` entry of each
   e2e project, and the `customConditions` entry of `tsconfig.base.json`, which becomes
   `["@absence-management/source"]`. The libraries already carry their final names.

2. **Fix the compiler options of the generated libraries.** They inherit `lib: ["es2022"]` and, for
   the `@nx/js` library, `moduleResolution: "nodenext"`. Everything here runs in a browser and is
   bundled by Vite, so add to each `tsconfig.lib.json`:

   ```json
   "lib": ["dom", "dom.iterable", "es2022"],
   "module": "esnext",
   "moduleResolution": "bundler"
   ```

   (the last two only for `packages/shared/api-client`; the React libraries already have them).

3. **Mark the React libraries private.** `@nx/js:library` writes `"private": true` into the
   `package.json` it generates, `@nx/react:library` does not — which makes Nx treat those libraries
   as publishable: it tags them `npm:public` and infers an `nx-release-publish` target. Nothing here
   is published to a registry, so add the field to each of them.

4. **Delete the Babel leftovers.** Generating a React library writes a `.babelrc` and pulls
   `@babel/core` and `@babel/preset-react` into the root `package.json`. Since
   `@vitejs/plugin-react` 6 does React Fast Refresh with Oxc, nothing reads them any more:

   ```bash
   Remove-Item -Recurse -Force -ErrorAction Ignore packages/*/*/.babelrc
   ```

   ```bash
   pnpm remove -w @babel/core @babel/preset-react
   ```

   The `"babel": true` that put them there sits in the `generators` section of `nx.json` from the
   creation command on, next to a `"linter": "eslint"` default. Both go, otherwise the next
   generator writes the files back.

5. **Drop the ESLint setup of the preset.** The workspace arrives with an `eslint.config.mjs` at
   the root and one in every project under `apps/` — the two from the creation command and the two
   from the application generator, which was not given `--linter=none` — plus an `@nx/eslint/plugin`
   entry in the `plugins` array of `nx.json` and eleven ESLint packages, nine of them for rules this
   workspace does not run. It lints with oxlint and keeps ESLint for exactly one rule, so the
   per-project configurations and the plugin entry go, and the root configuration is replaced by the
   one in [Enforced boundaries](#enforced-boundaries):

   ```bash
   Remove-Item -Force -ErrorAction Ignore apps/*/eslint.config.mjs
   ```

   ```bash
   pnpm remove -w @nx/eslint @eslint/js typescript-eslint eslint-config-prettier eslint-plugin-import eslint-plugin-jsx-a11y eslint-plugin-playwright eslint-plugin-react eslint-plugin-react-hooks
   ```

   What stays is `eslint`, `@nx/eslint-plugin` — which is where `@nx/enforce-module-boundaries`
   comes from — and the `@typescript-eslint/parser` added above, because that one rule still has to
   read TypeScript and JSX.

6. **Remove the routing scaffold.** `--routing` defaults to `true` and was passed explicitly to
   the application generator, so `react-router-dom` is a dependency of the root `package.json` and
   both applications carry it: a `<Routes>` block with two example routes in `src/app/app.tsx`, a
   `<BrowserRouter>` around the tree in `src/main.tsx`, and an `app.spec.tsx` that renders through
   it and asserts the text of the Nx welcome page. The pages come from the feature libraries here,
   and neither application routes:

   ```bash
   pnpm remove -w react-router-dom
   ```

   With that gone, `app.tsx`, `main.tsx` and `app.spec.tsx` of both applications are down to a
   shell, a `createRoot` call and a render test. `--routing=false` on the generator leaves less to
   undo.

7. **Point Nx at the right base branch.** `nx.json` is written with `"defaultBase": "master"` while
   the branch of this repository is `main`, so every `nx affected` comparison resolves against a ref
   that does not exist. Set `"defaultBase": "main"` — `--defaultBase=main` on the creation command
   does the same thing one step earlier.

8. **Give the e2e projects the Node types.** `apps/*-e2e/tsconfig.json` is generated without a
   `types` entry, while the Playwright configuration next to it reads `process.env` and
   `import.meta.dirname`. `pnpm typecheck` fails on both until its compiler options say:

   ```json
   "types": ["node"]
   ```

9. **Take the Vite configuration out of the spec projects.** `apps/*/tsconfig.spec.json` includes
   `vite.config.mts`, and that file imports `vite.shared.mts` from the workspace root — outside the
   `rootDir` a composite project derives from its own folder, which is TS6059 plus TS6307. Remove
   the `vite.config.*` and `vitest.config.*` entries from the `include` of both files. Vite never
   typechecks its own configuration anyway, and a broken one fails loudly on the next `nx dev`.

10. **Tag every project** in its `package.json` — see [Enforced boundaries](#enforced-boundaries).

11. **Write the shared configuration files** — see [Shared configuration](#shared-configuration).

12. **Write the tooling configuration** — see [The Oxc toolchain](#the-oxc-toolchain).

13. **Reduce the generated `.gitignore`.** The creation command writes the workspace-wide file
    every Nx workspace gets, and most of it is history: Eclipse and Cloud9 project files, a Sass
    cache, `testem.log`, `typings`. What is kept are the paths this toolchain really produces: the
    dependencies, the build output, the Nx cache, the timestamp files Vite writes next to a
    configuration it had to transpile, the Playwright and coverage output, and `.vscode/*` with the
    four shared files excepted. Nothing else belongs here — what is not generated by `pnpm` stays
    in the [.gitignore](#gitignore) of the repository root, which is where the two entries of the
    coding agents were moved to as well, so that they cover the whole repository and not only
    `frontend/`.

14. **Correct the editor recommendations.** `.vscode/extensions.json` is generated with
    `esbenp.prettier-vscode` in it, a formatter this workspace does not use. It is replaced by
    `oxc.oxc-vscode`, the Oxc extension, which shows the oxlint diagnostics while typing and
    formats with oxfmt. `dbaeumer.vscode-eslint` stays next to it, because ESLint is still what
    runs the one boundary rule; Nx Console and the Playwright extension are kept as generated. The
    file is the only thing under `.vscode/` that is committed.

The `nx-welcome.tsx` component and its import in `app.tsx` can be deleted from both applications,
together with the `app.module.css` next to it; `styles.css` stays, it is where the global stylesheet
import lands.

The generators also write a `README.md` into the workspace root and into every library, all of them
Nx boilerplate that describes a workspace this is not — `nx serve web`, `project.json`, unit tests
run with Jest. They are replaced with a short description of what each project actually holds.

### Project structure

The state at the end of this section:

```text
frontend/
├─ apps/
│  ├─ web/                                  employee application
│  │  ├─ src/
│  │  │  ├─ app/app.tsx                     which layout, which pages - nothing else
│  │  │  ├─ main.tsx
│  │  │  └─ styles.css
│  │  ├─ index.html
│  │  ├─ package.json                       nx.tags + the workspace:* dependencies
│  │  ├─ tsconfig.app.json
│  │  ├─ tsconfig.json
│  │  ├─ tsconfig.spec.json
│  │  └─ vite.config.mts                    three lines, the rest is in vite.shared.mts
│  ├─ web-e2e/                              Playwright, drives the built web app
│  │  ├─ src/*.spec.ts
│  │  ├─ package.json                       nx.tags + implicitDependencies: web
│  │  └─ playwright.config.mts
│  ├─ admin/                                approver application, same shape
│  └─ admin-e2e/
├─ packages/
│  ├─ shared/
│  │  ├─ api-client/                        scope:shared, type:util
│  │  │  ├─ package.json                    also holds the generate-api target
│  │  │  └─ src/
│  │  │     ├─ generated/                   written by `pnpm gen:api`, checked in, never edited
│  │  │     ├─ lib/api-error.ts             the error type the whole frontend catches
│  │  │     └─ lib/client.ts                problem details -> ApiError interceptor
│  │  ├─ i18n/                              scope:shared, type:util
│  │  │  └─ src/lib/
│  │  │     ├─ en.ts                        English, the reference language
│  │  │     ├─ de.ts                        German, `satisfies typeof en`
│  │  │     └─ i18n.ts                      the i18next instance and the key type
│  │  └─ ui/                                scope:shared, type:ui
│  │     ├─ .storybook/                     main.ts and preview.tsx, the Storybook setup
│  │     └─ src/lib/
│  │        ├─ absence-labels.ts            enum value -> translation key and colour
│  │        ├─ absence-request-table.tsx    presentational table, actions are callbacks
│  │        ├─ absence-request-table.stories.tsx  one story per state
│  │        ├─ app-layout.tsx               providers, header, language switcher
│  │        ├─ employee-table.tsx           presentational table, its states are props
│  │        ├─ employee-table.stories.tsx   one story per state
│  │        ├─ language-switcher.tsx        DE/EN, sits in the header
│  │        └─ status-badge.tsx             the status as a coloured badge
│  ├─ employees/
│  │  ├─ data-access/                       scope:employees, type:data-access
│  │  │  └─ src/lib/use-employees.ts        the employee list
│  │  └─ feature/                           scope:employees, type:feature
│  │     └─ src/lib/employees-page.tsx      title + EmployeeTable
│  └─ absences/
│     ├─ data-access/                       scope:absences, type:data-access
│     │  └─ src/lib/use-absence-requests.ts  list, create, update, approve, reject
│     └─ feature/                           scope:absences, type:feature
│        └─ src/lib/
│           ├─ absence-request-form.tsx     one form for create and edit
│           └─ absences-page.tsx            list, dialog, decisions
├─ openapi/
│  └─ AbsenceManagement.Api.json            written by `dotnet build`, checked in
├─ .editorconfig                            written by create-nx-workspace, also read by oxfmt
├─ .gitignore                               only what the JavaScript toolchain generates
├─ .oxfmtrc.json                            oxfmt
├─ .oxlintrc.json                           oxlint
├─ eslint.config.mjs                        one rule: @nx/enforce-module-boundaries
├─ nx.json                                  the Nx plugins and their target names
├─ openapi-ts.config.ts                     how the API client is generated
├─ package.json                             every runtime dependency, and the scripts
├─ pnpm-workspace.yaml                      the package globs (Nx) and the pnpm options (by hand)
├─ postcss.config.cjs                       Mantine's PostCSS preset
├─ tsconfig.base.json                       compiler options shared by every project
├─ tsconfig.json                            project references, kept in step by `nx sync`
├─ vite.shared.mts                          the Vite setup both applications share
├─ vitest.config.ts                         collects the per-project Vitest configurations
└─ vitest.setup.ts                          the browser APIs jsdom does not implement
```

Eleven projects, two axes of tags. **Tags live in the `nx` key of each `package.json`**, there is no
`project.json`:

```json
{
  "name": "@absence-management/shared-ui",
  "version": "0.0.1",
  "private": true,
  "nx": { "tags": ["scope:shared", "type:ui"] }
}
```

| Project                          | Tags                                  | Holds                                            |
| -------------------------------- | ------------------------------------- | ------------------------------------------------ |
| `apps/web`, `apps/admin`         | `scope:app`, `type:app`               | Only a shell: providers and which pages to mount |
| `apps/web-e2e`, `apps/admin-e2e` | `scope:app`, `type:e2e`               | Playwright specs, no imports from the workspace  |
| `packages/shared/api-client`     | `scope:shared`, `type:util`           | The generated client and `ApiError`              |
| `packages/shared/i18n`           | `scope:shared`, `type:util`           | The English and German texts, and i18next        |
| `packages/shared/ui`             | `scope:shared`, `type:ui`             | Presentational Mantine components, no API calls  |
| `packages/employees/data-access` | `scope:employees`, `type:data-access` | Query hooks and types of the employees area      |
| `packages/employees/feature`     | `scope:employees`, `type:feature`     | Pages and forms of the employees area            |
| `packages/absences/data-access`  | `scope:absences`, `type:data-access`  | Query hooks and types of the absences area       |
| `packages/absences/feature`      | `scope:absences`, `type:feature`      | Pages and forms of the absences area             |

The layout mirrors the backend: a feature area on the frontend is what a module is on the backend,
and `type:` is what the four project layers are inside a module.

### Shared configuration

**`package.json`** (root) — the scripts and the pinned package manager:

```json
"packageManager": "pnpm@11.22.0",
"engines": { "node": ">=22.12" },
"scripts": {
  "dev": "nx dev @absence-management/web",
  "dev:admin": "nx dev @absence-management/admin",
  "build": "pnpm gen:api && nx run-many -t build",
  "gen:api": "nx run @absence-management/shared-api-client:generate-api",
  "test": "nx run-many -t test",
  "e2e": "nx run-many -t e2e",
  "typecheck": "nx run-many -t typecheck --args=--force",
  "lint": "oxlint",
  "lint:fix": "oxlint --fix",
  "boundaries": "eslint .",
  "format": "oxfmt",
  "format:check": "oxfmt --check",
  "check": "pnpm typecheck && pnpm lint && pnpm boundaries && pnpm format:check",
  "graph": "nx graph",
  "storybook": "nx run @absence-management/shared-ui:storybook",
  "build-storybook": "nx run @absence-management/shared-ui:build-storybook"
}
```

`pnpm run dev` and `pnpm run dev:admin` are what the two `AddViteApp(…)` resources of the AppHost
call, and `pnpm run gen:api` is what the `api-client` resource calls, so Aspire starts Nx without
knowing about Nx. The `dev` scripts contain nothing but `nx dev <app>`: anything both applications
need happens once, in the `api-client` resource.

`typecheck` passes `--args=--force` because it runs `tsc --build`. Without the flag, TypeScript's
incremental state can report success right after `pnpm gen:api` rewrote the generated types
underneath it — a false pass on exactly the check that matters most here.

`lint` and `boundaries` are two commands on purpose, see [The Oxc toolchain](#the-oxc-toolchain).

**`pnpm-workspace.yaml`** — the `packages:` globs are maintained by Nx, one line per folder that
holds projects; `autoInstallPeers` and three of the `allowBuilds` entries come from the creation
command, the two remaining entries and `confirmModulesPurge` are added by hand:

```yaml
# The first glob comes with the workspace, the others are written by the Nx generators - a new
# project in a folder that is not covered yet appends a line here. Every folder that matches gets
# its own node_modules with exactly the dependencies its package.json declares - nothing else is
# reachable.
packages:
  - 'apps/*'
  - 'packages/employees/*'
  - 'packages/shared/*'
  - 'packages/absences/*'

autoInstallPeers: true

# pnpm blocks install scripts of dependencies unless they are decided here. The first three lines
# are written by Nx - Vite 8 does not use esbuild any more, so its install script is not needed.
# oxlint and oxfmt unpack a native binary and genuinely need theirs; everything else stays blocked,
# which is the point. `pnpm approve-builds` maintains the list.
allowBuilds:
  nx: true
  '@swc/core': false
  esbuild: false
  oxlint: true
  oxfmt: true

# node_modules records the absolute path it was created at. Move or rename the repository folder
# and pnpm wants to purge and rebuild it - which it asks about first, and Aspire runs
# `pnpm install` with no TTY to answer on. The install then aborts with exit code 1 and the
# "web" resource never starts.
confirmModulesPurge: false
```

**`postcss.config.cjs`** — Mantine's preset, one file for the whole workspace:

```javascript
// Mantine ships plain CSS with custom functions (light-dark(), rem(), breakpoint queries).
// This preset resolves them at build time, which is why Mantine needs no runtime CSS engine.
module.exports = {
  plugins: {
    'postcss-preset-mantine': {},
    'postcss-simple-vars': {
      variables: {
        'mantine-breakpoint-xs': '36em',
        'mantine-breakpoint-sm': '48em',
        'mantine-breakpoint-md': '62em',
        'mantine-breakpoint-lg': '75em',
        'mantine-breakpoint-xl': '88em',
      },
    },
  },
};
```

**`vite.shared.mts`** — the Vite setup both applications share. The generators write a full config
into each app; replace both with a call to this:

```ts
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
// Vite's own UserConfig has no `test` key. Vitest 4 no longer augments it through a
// /// <reference types='vitest' /> - it exports its own type instead.
import type { UserConfig } from 'vitest/config';

interface AppConfigOptions {
  /** The Nx project name, used for the Vitest project name and the Vite cache directory. */
  name: string;
  /** `import.meta.dirname` of the app's own vite.config.mts. */
  root: string;
  /** Port used when the app is started outside Aspire (`nx dev <app>`). */
  defaultPort: number;
}

/**
 * Two values are injected by the Aspire AppHost when the system is started with `aspire run`:
 *   PORT     - the port Aspire allocated for this resource
 *   API_URL  - the address of the "api" resource
 * The fallbacks let each app also run standalone against the API's launch profile.
 *
 * This lives in one file so the two apps cannot end up with different proxy rules - the kind of
 * difference that only shows up as "works in web, 404 in admin".
 */
export function createAppConfig({
  name,
  root,
  defaultPort,
}: AppConfigOptions): UserConfig {
  const workspaceRoot = resolve(root, '../..');
  const port = Number(process.env.PORT) || defaultPort;
  const apiUrl = process.env.API_URL ?? 'http://localhost:5180';
  const shortName = name.split('/').pop();

  return {
    root,
    cacheDir: `${workspaceRoot}/node_modules/.vite/apps/${shortName}`,
    // The Vite root is the app folder, so PostCSS would look for its config there. Mantine's
    // preset is workspace-wide, hence the explicit path to postcss.config.cjs at the root.
    css: { postcss: workspaceRoot },
    server: {
      port,
      host: 'localhost',
      proxy: {
        // The apps always call /api on their own origin, which avoids any CORS setup.
        '/api': { target: apiUrl, changeOrigin: true },
      },
    },
    preview: { port, host: 'localhost' },
    plugins: [react()],
    build: { outDir: './dist', emptyOutDir: true },
    test: {
      name,
      watch: false,
      globals: true,
      environment: 'jsdom',
      setupFiles: [`${workspaceRoot}/vitest.setup.ts`],
      include: ['{src,tests}/**/*.{test,spec}.{ts,mts,tsx}'],
      coverage: { reportsDirectory: './test-output/vitest/coverage', provider: 'v8' as const },
    },
  };
}
```

`plugins: [react()]` is the whole React setup. On Vite 8 `@vitejs/plugin-react` uses Oxc for the
Fast Refresh transform, so there is no Babel in the dev server and none in the build.

**`vitest.setup.ts`** (root) — jsdom implements neither `matchMedia` nor `ResizeObserver`, and
Mantine calls the first one while it mounts. The file stubs both, and `setupFiles` above loads it
for every project, so a test that renders a page fails on its assertion rather than on the
environment.

The import of this file out of `apps/*/vite.config.mts` crosses a project boundary by path, which
is why it is in the `allow` list of the boundary rule below and why `vite.config.mts` is no longer
part of the spec project (step 9 above).

**`tsconfig.json`** (root) — the `references` array is not written by hand. The generators add an
entry per project, and `nx sync` keeps it and the per-project references in step with the package
dependencies, which is the third of the three boundary checks below.

### The Oxc toolchain

Oxc replaces the JavaScript-based tools with Rust ones. Four of the five replacements drop in
without friction; the fifth is the reason ESLint does not disappear completely:

| Was                          | Is now           | How it arrives                                 |
| ---------------------------- | ---------------- | ---------------------------------------------- |
| esbuild (transform, resolve) | Oxc              | Built into Vite 8, nothing to install           |
| Rollup (bundle)              | Rolldown         | Built into Vite 8, nothing to install           |
| Babel (React Fast Refresh)   | Oxc              | `@vitejs/plugin-react` 6, nothing to configure  |
| ESLint (code quality)        | oxlint           | `.oxlintrc.json`                               |
| Prettier                     | oxfmt            | `.oxfmtrc.json`                                |
| ESLint (architecture rule)   | **still ESLint** | `eslint.config.mjs`, one rule                  |

Vite 8 and `@vitejs/plugin-react` 6 are installed by the creation command, so the first three rows
need no decision at all — they are what a current Nx workspace already builds with. Only the last
three are configuration.

**`.oxlintrc.json`**:

```jsonc
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  // Setting `plugins` replaces the default set, so the defaults are repeated here. `vitest` is
  // not among them: its rules are about test files and would otherwise fire on production code.
  "plugins": ["eslint", "typescript", "unicorn", "oxc", "react", "jsx-a11y", "import", "promise"],
  /*
   * oxlint exits with a non-zero status for warnings as well as for errors, so there is no
   * "reported but tolerated" level: every enabled category has to be one this workspace actually
   * follows. `style` is off for that reason - it wants single default exports, sorted object
   * keys, capitalised comments and `const` function expressions, none of which is how this
   * codebase or the Nx generators write code. The three categories that stay find defects.
   */
  "categories": {
    "correctness": "error",
    "suspicious": "error",
    "perf": "error",
    "style": "off"
  },
  "env": { "browser": true, "es2022": true },
  "rules": {
    // The frontend talks to the API through the generated client only.
    "no-restricted-globals": ["error", "fetch", "XMLHttpRequest"],
    "no-console": "error",
    // Mantine ships plain CSS, so a stylesheet is imported for its side effect - by AppLayout
    // once per app, and by the Storybook preview.
    "import/no-unassigned-import": ["error", { "allow": ["**/*.css"] }],
    // React 19 with the automatic JSX runtime - nothing imports React itself.
    "react/react-in-jsx-scope": "off"
  },
  // Generated code is not reviewed, formatted or linted - it is regenerated.
  "ignorePatterns": ["packages/shared/api-client/src/generated", "openapi"],
  "overrides": [
    {
      "files": ["**/*.test.ts", "**/*.test.tsx", "**/*.spec.ts", "**/*.spec.tsx"],
      "plugins": ["vitest"],
      "rules": { "no-console": "off" }
    }
  ]
}
```

Everything oxlint needs comes out of `node_modules`; `dist`, `.nx`, `test-output` and the rest are
skipped because oxlint honours `.gitignore` by default.

The two comments in that file come down to one property of the tool: oxlint exits non-zero for
warnings as well as for errors, so a category is either enforced or off. `style` would fail
`pnpm check` over unsorted object keys and named exports, and the `vitest` rules would fire on
production code (`require-hook` on a `createRoot` call, for instance). The override at the bottom
switches `vitest` back on for `*.spec.*` and `*.test.*` files only.

The `react` plugin covers `eslint-plugin-react`, `eslint-plugin-react-hooks` and React Refresh, so
the hook rules that the ESLint setup of a generated Nx workspace provides are still there.

Type-aware rules (`oxlint --type-aware`, plus the `oxlint-tsgolint` package) are **not** enabled.
They run on `tsgo` and need TypeScript 7, and this workspace cannot move there yet: Nx 23 scaffolds
TypeScript 6, and `@typescript-eslint/parser` 8 — the parser the one remaining ESLint rule runs on —
declares `typescript` as `>=4.8.4 <6.1.0`. Turn them on when both catch up; `pnpm typecheck` covers
the same ground in the meantime.

**`.oxfmtrc.json`**:

```jsonc
{
  "singleQuote": true,
  "ignorePatterns": ["packages/shared/api-client/src/generated", "openapi"]
}
```

`tabWidth` is not repeated here: oxfmt reads `frontend/.editorconfig` and maps its `indent_size`
onto it, so editors and formatter work from one value. That file comes from the creation command
with `root = true`, which is what keeps the repository-wide `.editorconfig` — four-space
indentation, 100 character lines, meant for the .NET side — out of this workspace. It sets no
`max_line_length`, so oxfmt's default line width applies. `oxfmt --migrate prettier` converts a
`.prettierrc` if one is left over.

**What stays on ESLint, and why.** The architecture rule of this workspace is
`@nx/enforce-module-boundaries`. It reads the Nx project graph, which no other linter can do —
oxlint has no equivalent, and `import/no-restricted-paths` is not implemented there either. So
ESLint stays, with a configuration that contains that one rule and nothing else. There is no rule
overlap between the two linters and therefore no need for `eslint-plugin-oxlint`, no shared plugin
list to keep in sync, and no second opinion about code style:

| Command           | Tool   | Checks                                                  |
| ----------------- | ------ | ------------------------------------------------------- |
| `pnpm lint`       | oxlint | Correctness, suspicious code, performance, hooks, accessibility, imports |
| `pnpm boundaries` | ESLint | Only which project may depend on which                  |

Both run over the whole workspace in one process instead of once per project, which is why neither
is an Nx target: they finish faster than the graph computation that caching them would need. The
`@nx/eslint/plugin` entry that the preset writes into `nx.json` — which would infer a per-project
`lint` target — was removed with the rest of its ESLint setup, in step 5 above.

### Enforced boundaries

A layer boundary that is only written down is a boundary that leaks. This workspace checks it three
times, at three different moments:

| Check                                        | Command           | Fails when                                                  |
| -------------------------------------------- | ----------------- | ----------------------------------------------------------- |
| Nx tags, via `@nx/enforce-module-boundaries` | `pnpm boundaries` | A file imports a project the tags do not allow              |
| pnpm `workspace:*` dependencies              | `pnpm install`    | A project imports a package it does not declare             |
| TypeScript project references                | `pnpm typecheck`  | The reference graph does not match the package dependencies |

The three overlap on purpose. The tags describe the intent and give the readable error message,
pnpm makes an undeclared import unresolvable at all, and `nx sync` keeps the TypeScript references
in step with both. This is where the package-based layout earns its place: in an integrated
workspace the second check does not exist, because one hoisted `node_modules` makes every package
importable from everywhere. It is the frontend counterpart of the project references on the backend
side.

**The rules.** The `type:` axis says which layer may use which:

| Layer (`type:`)    | May depend on               | Never                                    |
| ------------------ | --------------------------- | ---------------------------------------- |
| `type:app`         | `feature`, `ui`, `util`     | `data-access` — an app makes no requests |
| `type:feature`     | `data-access`, `ui`, `util` | another `feature`                        |
| `type:data-access` | `util`                      | `ui`, `feature`                          |
| `type:ui`          | `util`                      | `data-access`, `feature`                 |
| `type:util`        | `util`                      | everything else                          |
| `type:e2e`         | `util`                      | everything else — it drives the built app over HTTP |

The `scope:` axis says which feature area may use which:

| Area (`scope:`)   | May depend on                     |
| ----------------- | --------------------------------- |
| `scope:app`       | `absences`, `employees`, `shared` |
| `scope:absences`  | `absences`, `employees`, `shared` |
| `scope:employees` | `employees`, `shared`             |
| `scope:shared`    | `shared`                          |

`absences` may reach into `employees` because the backend allows the same edge: the absences module
asks the employees module for the employee list. The edge is one directional — `employees` may not
depend on `absences` — and because both axes apply at once, the only import that actually passes is
`absences-feature` → `employees-data-access`. A feature importing another area's *pages* fails,
which is the case worth preventing.

**`eslint.config.mjs`** (root, and after step 5 above the only ESLint file in the workspace). It
replaces the one the creation command generated:

```js
import nx from '@nx/eslint-plugin';
import tsParser from '@typescript-eslint/parser';

export default [
  // Registers the @nx plugin. It brings no rules of its own, which is the point:
  // code style belongs to oxlint, this file is only about the dependency graph.
  ...nx.configs['flat/base'],
  {
    ignores: [
      '**/dist',
      '**/out-tsc',
      '**/test-output',
      '**/storybook-static',
      // Nx keeps a copy of every cached task output here, generated client included.
      '**/.nx/**',
      // The trailing /** is what makes ESLint skip the whole folder: without it the generated
      // files are linted and their eslint-disable comments name rules this configuration does
      // not load, which is an error of its own.
      'packages/shared/api-client/src/generated/**',
      '**/vite.config.*.timestamp*',
    ],
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.mjs'],
    // flat/base sets no parser, and the default one cannot read TypeScript or JSX.
    languageOptions: { parser: tsParser, ecmaVersion: 2024, sourceType: 'module' },
    rules: {
      /*
       * The frontend counterpart of the backend's project references: checked by
       * `pnpm boundaries`, so an accidental import across a layer or area boundary fails
       * the build instead of quietly creating a tangle.
       *
       *   scope:*  - which feature area a library belongs to (absences, employees, shared)
       *   type:*   - which layer it is (app, feature, data-access, ui, util, e2e)
       */
      '@nx/enforce-module-boundaries': [
        'error',
        {
          // Every library here is source based - its package.json entry points at src/index.ts
          // and the applications bundle it with Vite, so no library has a build target. With this
          // flag on, the first import from an application would fail as a 'buildable library
          // importing a non-buildable one'.
          enforceBuildableLibDependency: false,
          // Two files are imported by path rather than by package name: the ESLint
          // configuration itself, and the Vite setup the two applications share.
          allow: ['^.*/eslint\\.config\\.[cm]?[jt]s$', '^.*/vite\\.shared\\.mjs$'],
          depConstraints: [
            // --- feature areas ------------------------------------------------
            {
              sourceTag: 'scope:app',
              onlyDependOnLibsWithTags: ['scope:absences', 'scope:employees', 'scope:shared'],
            },
            {
              sourceTag: 'scope:absences',
              onlyDependOnLibsWithTags: ['scope:absences', 'scope:employees', 'scope:shared'],
            },
            {
              sourceTag: 'scope:employees',
              onlyDependOnLibsWithTags: ['scope:employees', 'scope:shared'],
            },
            { sourceTag: 'scope:shared', onlyDependOnLibsWithTags: ['scope:shared'] },
            // --- layers -------------------------------------------------------
            {
              sourceTag: 'type:app',
              onlyDependOnLibsWithTags: ['type:feature', 'type:ui', 'type:util'],
            },
            {
              sourceTag: 'type:feature',
              onlyDependOnLibsWithTags: ['type:data-access', 'type:ui', 'type:util'],
            },
            { sourceTag: 'type:data-access', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:ui', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:util', onlyDependOnLibsWithTags: ['type:util'] },
            { sourceTag: 'type:e2e', onlyDependOnLibsWithTags: ['type:util'] },
          ],
        },
      ],
    },
  },
];
```

**The `workspace:*` dependencies.** This is the part npm's hoisting lets you skip. pnpm gives every
package a `node_modules` with exactly its declared dependencies, so a package that imports another
one has to say so in its own `package.json`:

| Package                          | `dependencies`                                                          |
| -------------------------------- | ----------------------------------------------------------------------- |
| `apps/web`                       | `absences-feature`, `employees-feature`, `shared-i18n`, `shared-ui`     |
| `apps/admin`                     | `absences-feature`, `employees-feature`, `shared-i18n`, `shared-ui`     |
| `packages/absences/feature`      | `absences-data-access`, `employees-data-access`, `shared-i18n`, `shared-ui` |
| `packages/employees/feature`     | `employees-data-access`, `shared-i18n`, `shared-ui`                     |
| `packages/absences/data-access`  | `shared-api-client`                                                     |
| `packages/employees/data-access` | `shared-api-client`                                                     |
| `packages/shared/ui`             | `shared-api-client`, `shared-i18n`                                      |
| `packages/shared/api-client`     | `shared-i18n`                                                           |
| `packages/shared/i18n`           | —                                                                        |

All of them with the version `workspace:*` and the `@absence-management/` prefix, for example
`apps/admin/package.json`:

```json
{
  "name": "@absence-management/admin",
  "version": "0.0.1",
  "private": true,
  "nx": { "tags": ["scope:app", "type:app"] },
  "dependencies": {
    "@absence-management/absences-feature": "workspace:*",
    "@absence-management/employees-feature": "workspace:*",
    "@absence-management/shared-i18n": "workspace:*",
    "@absence-management/shared-ui": "workspace:*"
  }
}
```

`workspace:*` means "the copy in this repository, whatever its version" — pnpm symlinks it and
refuses to install anything from the registry under that name. The payoff: an import that the
dependency graph does not allow fails at install time, not at review time.

The two e2e projects declare no dependencies. They reach their application over HTTP, not through
an import, so the generator gives them an `implicitDependencies` entry instead — which is what
makes `nx affected` rerun them when the application changes:

```json
"nx": {
  "tags": ["scope:app", "type:e2e"],
  "implicitDependencies": ["@absence-management/web"]
}
```

Install everything and let Nx write the TypeScript references:

```bash
pnpm install
```

```bash
pnpm exec nx sync
```

```bash
pnpm check
```

```bash
pnpm exec nx graph
```

`nx graph` opens the dependency graph in the browser — the frontend equivalent of the project
reference diagram on the backend side. Adding a forbidden import to check that the boundary really
holds is worth the two minutes:

```text
$ pnpm boundaries
  error  A project tagged with "type:ui" can only depend on projects tagged with "type:util"
     @nx/enforce-module-boundaries
```

### The generated API client

The one place where backend and frontend really meet is the HTTP contract. Writing the TypeScript
types by hand means a renamed C# property compiles fine on both sides and renders `undefined` in
the browser. So neither the contract nor the client is written by hand:

```text
C# endpoint + .WithName() + .Produces<T>()
        │  dotnet build
        ▼
frontend/openapi/AbsenceManagement.Api.json          generated, checked in
        │  pnpm gen:api
        ▼
frontend/packages/shared/api-client/src/generated/   generated, checked in
```

**Step 1 — the backend writes the document on every build.** The document has to be written to a
path outside the project, so `Directory.Build.props` gets the repository root as a property:

```xml
<!-- The repository root, for the few paths that need to point outside their own project. -->
<PropertyGroup>
  <RepoRoot>$(MSBuildThisFileDirectory)</RepoRoot>
</PropertyGroup>
```

```bash
dotnet add src/Host/AbsenceManagement.Api package Microsoft.Extensions.ApiDescription.Server
```

Mark it build-only with `PrivateAssets="all"` and point it at the frontend, in
`AbsenceManagement.Api.csproj`:

```xml
<PropertyGroup>
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiGenerateDocumentsOnBuild>true</OpenApiGenerateDocumentsOnBuild>
  <OpenApiDocumentsDirectory>$(RepoRoot)frontend/openapi</OpenApiDocumentsDirectory>
</PropertyGroup>
```

The document lives under `frontend/` on purpose: the frontend build never needs a .NET SDK, and Nx
can use the file as a cache input.

To read the routes, the tool starts the host in a process of its own (`GetDocument.Insider`) and
stops it right after `builder.Build()`. Nothing is served and no connection is opened — but the
modules still run their registration and fail fast when a connection string is missing. That
fail-fast is worth keeping, so `Program.cs` hands out a placeholder instead of weakening the check,
through the `AddPlaceholderConnectionStrings` call that is already there. **Every new module has to
be added to that call**, otherwise `dotnet build` fails.

**Step 2 — endpoints describe themselves.** This is already the case: every endpoint carries
`.WithName(…)`, `.Produces<T>(…)` and `.ProducesProblems(…)`. The name becomes the TypeScript
function name, the types become its signature. Anonymous objects (`new { id }`) do not appear in
the document, which is why the create endpoints return a declared response record such as
`CreateEmployeeResponse`.

**Step 3 — the frontend generates its client.** `frontend/openapi-ts.config.ts`:

```ts
import { defineConfig } from '@hey-api/openapi-ts';

/**
 * Generates the typed API client from the OpenAPI document.
 *
 * The input file is written by `dotnet build` of AbsenceManagement.Api (see
 * OpenApiDocumentsDirectory in its csproj). Run with `pnpm gen:api`. The output is checked in,
 * so a fresh clone builds without codegen.
 */
export default defineConfig({
  input: './openapi/AbsenceManagement.Api.json',
  output: {
    path: './packages/shared/api-client/src/generated',
    // Everything here is bundled by Vite, so extensionless relative imports are the least
    // surprising. The folder is excluded from oxlint, oxfmt and ESLint - it is regenerated,
    // not maintained.
    importFileExtension: '',
  },
  plugins: [
    {
      name: '@hey-api/client-fetch',
      // The frontend always calls /api on its own origin; the Vite dev server proxies it.
      baseUrl: '',
      // Reject instead of returning an error object, which is what TanStack Query wants.
      throwOnError: true,
    },
    {
      // Enums become const objects, so `AbsenceType.VACATION` exists at runtime and the list of
      // types in the UI is derived from the contract instead of retyped.
      name: '@hey-api/typescript',
      enums: 'javascript',
    },
    '@hey-api/sdk',
  ],
});
```

The Nx target goes into `packages/shared/api-client/package.json` — in a package-based workspace
that is where a project's targets live — so the generation is cached and only re-runs when the
document or the config changes:

```json
"nx": {
  "tags": ["scope:shared", "type:util"],
  "targets": {
    "generate-api": {
      "executor": "nx:run-commands",
      "cache": true,
      "inputs": [
        "{workspaceRoot}/openapi/AbsenceManagement.Api.json",
        "{workspaceRoot}/openapi-ts.config.ts"
      ],
      "outputs": ["{projectRoot}/src/generated"],
      "options": { "command": "openapi-ts", "cwd": "{workspaceRoot}" }
    }
  }
}
```

`build` may chain the generation (`pnpm gen:api && nx run-many -t build`) because it is one
process. The `dev` scripts may not — two of them run at the same time, and the loser of the race
dies with `ENOENT` because the winner cleared the output folder underneath it. That is the whole
reason the AppHost has a separate `api-client` resource.

**Step 4 — a failed request becomes one error type.** The generated `ProblemDetails` type has no
`code` property: `code` is an RFC 9457 extension member and extensions are not part of the schema.
One error interceptor in `packages/shared/api-client/src/lib/client.ts` reads the body and returns
the hand-written `ApiError`, which the client then throws because it is configured with
`throwOnError`:

```ts
client.interceptors.error.use((error, response): ApiError => {
  const problem = error as ProblemDetailsWithCode | null;
  const detail = typeof problem?.detail === 'string' ? problem.detail : undefined;

  return new ApiError(detail ?? i18next.t('errors.unexpected'), response?.status, problem?.code);
});
```

`detail` is the message the domain wrote, so a component renders `error.message` and shows the
business rule that was broken; `code` (`Absences.Overlapping`) is kept for the day a message
catalogue keys on it. Those two files are the only hand-written code in the package —
`src/index.ts` re-exports them next to the generated client, and importing `./lib/client` is what
registers the interceptor.

**Step 5 — keep the generated files out of diffs.** Two lines in the repository's `.gitattributes`,
next to the lockfile rules:

```gitattributes
# Generated from the C# endpoints, regenerated by `dotnet build` and `pnpm gen:api`.
frontend/openapi/*.json                                    linguist-generated=true -diff
frontend/packages/shared/api-client/src/generated/**       linguist-generated=true -diff
```

**Checking it works.** Rename a property in `EmployeeDto`, then:

```bash
dotnet build
```

```bash
cd frontend; pnpm gen:api; pnpm check
```

Once the pages below exist, `pnpm check` names the exact `.tsx` line that still uses the old name.
In CI the same commands plus `git diff --exit-code` catch a stale checked-in client.

### Application: web

The employee application. An app is only a shell: it picks a layout and mounts the pages of the
feature libraries.

**The code in this section and the next one is the frontend implementation, which the bootstrap
steps do not produce.** What the steps above produce is the wiring: `vite.config.mts`,
`index.html` with the application title, an `app.tsx` that renders an empty `<main />`, a render
test, and libraries whose `src/index.ts` holds nothing but a comment. The workspace typechecks,
lints, builds, tests and runs in that state; the pages below arrive with the implementation.

`apps/web/vite.config.mts` — the generated file is replaced by:

```ts
import { defineConfig } from 'vite';
import { createAppConfig } from '../../vite.shared.mjs';

export default defineConfig(() =>
  createAppConfig({
    name: '@absence-management/web',
    root: import.meta.dirname,
    defaultPort: 4200,
  }),
);
```

The import ends in `.mjs` although the file is `vite.shared.mts` — that is what
`moduleResolution: "bundler"` expects for a TypeScript ESM module.

`apps/web/src/app/app.tsx`:

```tsx
import { AbsencesPage } from '@absence-management/absences-feature';
import { useTranslation } from '@absence-management/shared-i18n';
import { AppLayout } from '@absence-management/shared-ui';

/**
 * The shell of the employee application: it picks a layout and mounts the pages of the feature
 * libraries - no Mantine component and no query hook of its own. Requests can be created and
 * edited here; deciding them belongs to the approver application.
 */
export function App() {
  const { t } = useTranslation();

  return (
    <AppLayout title={t('app.employeeTitle')} accentColor="teal">
      <AbsencesPage canAddRequest canEditRequest />
    </AppLayout>
  );
}

export default App;
```

`AppLayout` lives in `packages/shared/ui` and is what both applications need before the first page
renders — the Mantine theme, the baseline styles, the query client, i18next and the header:

```tsx
import '@mantine/core/styles.css';
import '@mantine/dates/styles.css';

import { i18next, I18nextProvider } from '@absence-management/shared-i18n';
import {
  AppShell,
  Container,
  createTheme,
  Group,
  MantineProvider,
  type MantineColor,
  Stack,
  Title,
} from '@mantine/core';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { type ReactNode, useMemo } from 'react';

import { LanguageSwitcher } from './language-switcher';

/**
 * This exists because there are two apps. Duplicating the provider setup per app is how the two
 * start to drift - different retry policies, different themes, one of them missing the Mantine
 * stylesheet. An app's app.tsx is down to "which layout, which pages".
 *
 * The two stylesheet imports belong here for the same reason: Mantine ships plain CSS, and it
 * has to be imported exactly once, before any component that uses it. The I18nextProvider is
 * here because importing the instance is what initialises it - once, before the first t() call.
 */

const queryClient = new QueryClient({
  defaultOptions: {
    // Business errors are shown to the user instead of being retried.
    queries: { retry: false, refetchOnWindowFocus: false },
    mutations: { retry: false },
  },
});

export interface AppLayoutProps {
  /** Shown in the header. Also the fastest way to see which app you are looking at. */
  title: string;
  /** Primary colour, deliberately different per app for the same reason. */
  accentColor: MantineColor;
  /** The pages of the app, stacked in the order they are written. */
  children: ReactNode;
}

export function AppLayout({ title, accentColor, children }: AppLayoutProps) {
  const theme = useMemo(() => createTheme({ primaryColor: accentColor }), [accentColor]);

  return (
    <I18nextProvider i18n={i18next}>
      <QueryClientProvider client={queryClient}>
        <MantineProvider theme={theme}>
          <AppShell header={{ height: 56 }} padding="md">
            <AppShell.Header>
              <Group h="100%" px="md" justify="space-between">
                <Title order={4}>{title}</Title>
                <LanguageSwitcher />
              </Group>
            </AppShell.Header>

            <AppShell.Main>
              <Container size="lg">
                <Stack gap="xl">{children}</Stack>
              </Container>
            </AppShell.Main>
          </AppShell>
        </MantineProvider>
      </QueryClientProvider>
    </I18nextProvider>
  );
}
```

The `Stack` around `children` is why an app can list several pages without importing a Mantine
component of its own, and the `LanguageSwitcher` is in the header for the same reason: both apps
get it without either of them asking for it.

`AppShell` here is Mantine's layout component; the wrapper is called `AppLayout` so the two names
stay apart.

Mantine needs no `ThemeProvider` per component and no CSS-in-JS runtime: the components ship their
own CSS, the theme is a set of CSS custom properties, and anything project specific is written as a
plain `*.module.css` next to the component that uses it.

`apps/web/index.html` gets the title of the application, `Absence Management`. It is the shell
around the React tree, so it is a static string and not a translation.

`apps/web-e2e/playwright.config.mts` was generated next to it and three values in it have to
follow: the `webServer.command` (`pnpm exec nx run @absence-management/web:preview`), and the
fallback `baseURL` and `webServer.url`, which the generator writes as port 4300 — the preview port
of the generated Vite configuration. `vite.shared.mts` serves the preview on the same port as the
dev server, so both become 4200, matching `defaultPort` above. The specs drive the built
application, so `pnpm e2e` builds first.

### Application: admin

The approver application. Same shape, two differences: another accent colour, and it mounts the
pages of both feature areas — it is the only kind of project allowed to.

`apps/admin/vite.config.mts` is identical except for `name` and `defaultPort: 4201`. Its e2e
project has to agree: in `apps/admin-e2e/playwright.config.mts` the fallback `baseURL`, the
`webServer.url` and the `webServer.command` all change to port 4201 and
`@absence-management/admin`. Two apps on one port is the mistake this pairing produces, and
Playwright's `reuseExistingServer` hides it by silently testing the wrong application.

`apps/admin/src/app/app.tsx`:

```tsx
import { AbsencesPage } from '@absence-management/absences-feature';
import { EmployeesPage } from '@absence-management/employees-feature';
import { useTranslation } from '@absence-management/shared-i18n';
import { AppLayout } from '@absence-management/shared-ui';

/**
 * The shell of the approver application: same shape as the employee application, another accent
 * colour, and it is the only kind of project allowed to mount pages of several feature areas.
 * `canDecideRequest` is what the employee application does not pass - here a request can be
 * approved or rejected, but not created or edited.
 */
export function App() {
  const { t } = useTranslation();

  return (
    <AppLayout title={t('app.adminTitle')} accentColor="indigo">
      <AbsencesPage canDecideRequest />
      <EmployeesPage />
    </AppLayout>
  );
}

export default App;
```

The props of `AbsencesPage` are the whole difference between the two applications: the same page,
with the approve and reject actions instead of the create and edit ones. That the two roles are
props and not a permission system is what the task asks for — authentication and roles are out of
scope.

Note what is *not* imported here: no Mantine component and no query hook. Layout and spacing belong
to `shared/ui`, requests belong to the `data-access` libraries — an app that starts importing
either is the first step of the drift the boundaries exist to prevent.

Both applications get their port from Aspire at runtime; `defaultPort` only applies when one is
started on its own with `pnpm run dev`.

### The absences pages

The vertical slice on the frontend side. It is three files plus two presentational components, and
where each of them lives follows from the tags:

| File                                                       | Layer              | Holds                                                    |
| ---------------------------------------------------------- | ------------------ | -------------------------------------------------------- |
| `packages/absences/data-access/src/lib/use-absence-requests.ts` | `type:data-access` | The list query and the four mutations                    |
| `packages/absences/feature/src/lib/absences-page.tsx`      | `type:feature`     | Title, "new request" button, the table, the dialog       |
| `packages/absences/feature/src/lib/absence-request-form.tsx` | `type:feature`     | One form for creating and for editing                    |
| `packages/shared/ui/src/lib/absence-request-table.tsx`     | `type:ui`          | The table, its three states, the per-row actions         |
| `packages/shared/ui/src/lib/status-badge.tsx`              | `type:ui`          | The status as a coloured badge                           |

**Every request goes through data-access.** The five hooks are the only place in the absences area
that calls the generated client, and they all work on the same list, so each mutation invalidates
one query key:

```ts
export const absenceRequestsQueryKey = ['absence-requests'] as const;

function useAbsenceRequestMutation<TVariables>(
  mutationFn: (variables: TVariables) => Promise<unknown>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    // onSettled, not onSuccess: a refused decision usually means somebody else changed the
    // request, so the list is refetched then too and shows what it really looks like now.
    onSettled: () => queryClient.invalidateQueries({ queryKey: absenceRequestsQueryKey }),
  });
}
```

The library also re-exports `AbsenceRequestDto`, `AbsenceType` and `AbsenceStatus` from the
generated client. That is what keeps the feature libraries off `shared/api-client`: a page depends
on its own area, and the area decides what of the contract it passes on.

**The rules are shown, not re-implemented.** The form validates only what the browser can decide —
a missing employee, a missing date, an end date before the start date (rule 1). Everything else is
the backend's answer: an overlap (rule 7) or a request that is no longer open (rules 5, 6 and 9)
comes back as a problem details document, becomes an `ApiError` in the interceptor and is rendered
in an `Alert`. The table offers *Edit*, *Approve* and *Reject* for open requests only,
so the decided ones cannot be sent into a rule they would break.

**One page, two applications.** `AbsencesPage` takes one boolean prop per action:

```tsx
<AbsenceRequestTable
  requests={data ?? []}
  isLoading={isPending}
  errorMessage={error?.message}
  isBusy={isDeciding}
  onEdit={canEditRequest ? setEditedRequest : undefined}
  onApprove={canDecideRequest ? (request) => approve.mutate(request.id) : undefined}
  onReject={canDecideRequest ? (request) => reject.mutate(request.id) : undefined}
/>
```

An action that is not passed is not rendered, so the employee application gets the list, the "new
request" button and the form (`canAddRequest canEditRequest`), and the approver application gets
the same list with the two decisions on it (`canDecideRequest`).

### Texts and languages

The frontend is English with a German translation, and every text of it lives in one library —
`packages/shared/i18n`, tagged `scope:shared`, `type:util`, so every other project may depend on it
and it may depend on nothing. That is the whole reason it is a library of its own and not a folder
inside `shared/ui`: the api-client needs it too, and `type:util` is the only layer both an
interceptor and a component are allowed to import. `i18next` and `react-i18next` were installed
with the other runtime dependencies above.

| File                | What it is                                                                 |
| ------------------- | --------------------------------------------------------------------------- |
| `src/lib/en.ts`     | English. The reference language: its shape defines the keys                 |
| `src/lib/de.ts`     | German, closed with `satisfies typeof en`                                   |
| `src/lib/i18n.ts`   | The i18next instance, the language list, and the `CustomTypeOptions` augmentation |
| `src/index.ts`      | Re-exports the instance, the provider and `useTranslation`, so nothing imports `react-i18next` directly |

**Type-safe keys, in two directions.** The keys are checked because i18next is told what the
resources look like:

```ts
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof en };
  }
}
```

`t('absences.titel')` is then a compile error with a "Did you mean `absences.title`?" next to it,
and `de.ts` cannot forget a key or invent one, because `satisfies typeof en` compares it against
English. Both are caught by `pnpm typecheck`, which means in CI, not in the browser.

The enums keep the two sides connected without a second list of names: `absences.type.Vacation`
and `absences.status.Open` are keyed by the values the API sends, so a label is looked up with
`t(absenceTypeKey(request.type))` and adding a type on the backend fails the typecheck here.

Even the date format is a translation (`formats.date`), so a date is written `08/24/2026` in
English and `24.08.2026` in German — the `dayjs` format string and Mantine's `valueFormat` both
read it from there.

**Switching.** `LanguageSwitcher` sits in the header of `AppLayout`, which both applications use,
and calls `i18n.changeLanguage`; every component that called `useTranslation` re-renders. Nothing
is persisted and no language is detected from the browser — the default is English, and a language
detector is one plugin away when it is wanted.

### Storybook

The components in `packages/shared/ui` are presentational — they render what they are handed and
make no request — so they can be developed and reviewed without an API, a database or an app
around them. Storybook is where that happens, and it is set up for that one library:

| Path                                | What it is                                                                  |
| ----------------------------------- | --------------------------------------------------------------------------- |
| `.storybook/main.ts`                | Which stories to load, the `@storybook/react-vite` framework, and the PostCSS path — Storybook's Vite root is the library, Mantine's preset is workspace-wide |
| `.storybook/preview.tsx`            | The Mantine stylesheet and a `MantineProvider` decorator: a story renders one component, so `AppLayout` is not in the tree |
| `src/lib/*.stories.tsx`             | One story per state of a component, next to the component itself            |

Two dev dependencies at the workspace root, `storybook` and `@storybook/react-vite`, and two
targets in `packages/shared/ui/package.json` — the same place the tags live. pnpm 11 refuses to
install packages younger than its minimum release age, so the Storybook 10.5.9 packages are listed
under `minimumReleaseAgeExclude` in `pnpm-workspace.yaml` until they grow past it:

```bash
pnpm storybook
```

```bash
pnpm build-storybook
```

The first serves the library on port 4400, the second writes a static site to
`packages/shared/ui/storybook-static` (git-ignored, and an Nx cache output). Sample data in a
story is typed against the generated client, so a renamed property in the contract fails
`pnpm typecheck` in the stories too.

### Frontend resources

Back in the AppHost for the last step: now that the Nx workspace exists, it gains three more
resources, the shared client generation and one Vite dev server per application. The package for
it:

```bash
dotnet add aspire/AbsenceManagement.AppHost package Aspire.Hosting.JavaScript
```

```csharp
// The frontend is one Nx workspace producing two applications, so the work they share happens
// once, in a resource of its own, before either of them starts:
//
//   1. Aspire builds the API project  -> frontend/openapi/AbsenceManagement.Api.json is rewritten
//   2. the API becomes healthy        -> "api-client" may start
//   3. `pnpm install`, then `pnpm run gen:api` regenerates the typed client, then it exits
//   4. "web" and "admin" start, both waiting for that exit code 0
var apiClient = builder.AddJavaScriptApp("api-client", "../../frontend", "gen:api")
    .WithPnpm()                                       // `pnpm install` happens here
    .WaitFor(api);

// Both apps share the node_modules that "api-client" has installed - hence WithPnpm(install: false).
// Each gets its own Vite dev server and its own port from Aspire.
builder.AddViteApp("web", "../../frontend")           // runs `pnpm run dev` -> `nx dev web`
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
```

This needs the `api` resource in a variable, so the `AddProject` call above becomes
`var api = builder.AddProject<AbsenceManagement_Api>("api")`.

`WithPnpm()` is what makes Aspire use pnpm instead of its npm default, without it the AppHost would
run `npm install` and quietly build a second, hoisted `node_modules` next to the pnpm one.

The third argument of `AddViteApp` / `AddJavaScriptApp` is the package script to run, which is how
one workspace serves two applications plus a build step.

Both methods come from `Aspire.Hosting.JavaScript`, which is part of Aspire itself. The community
toolkit offers an `AddNxApp` that would model the whole workspace as one resource; it is not used
here, because it would put a third-party package between the AppHost and the dev servers for
something the official API already expresses.

**Exactly one resource may install, and exactly one may generate.** Both apps share a single
`node_modules` and a single generated-client folder. Two `pnpm install` runs in the same directory
race; so do two `openapi-ts` runs, and the loser dies with `ENOENT` because the winner cleared the
output folder underneath it. `api-client` does both jobs once and exits; the apps `WaitForCompletion`
on it. That is also why the `dev` scripts are plain `nx dev <app>` with no codegen in them.

Aspire injects `PORT` and `API_URL` into each Vite process; both `vite.config.mts` files read them
through `frontend/vite.shared.mts` and proxy `/api` to the backend. That is the whole integration,
no CORS, no hardcoded ports.

### Run it

The frontend is part of the Aspire graph, so the normal way to start it is the same one command as
before, now with the three resources added just above:

```bash
aspire run
```

The dashboard then lists `api-client` (runs once and exits), `web` and `admin`. Both applications
get their port and their `API_URL` from Aspire, so nothing has to be looked up or configured.

Without Aspire, for frontend-only work, with the API running on its launch profile port 5180:

```bash
cd frontend; pnpm run dev
```

The checks, all of which also belong in CI:

```bash
pnpm check
```

| Command             | Checks                                                              |
| ------------------- | ------------------------------------------------------------------- |
| `pnpm typecheck`    | Every project compiles, including against the generated client      |
| `pnpm lint`         | oxlint over the whole workspace                                     |
| `pnpm boundaries`   | No import crosses a layer or feature-area boundary                  |
| `pnpm format:check` | oxfmt                                                               |
| `pnpm test`         | Vitest, per project                                                 |
| `pnpm e2e`          | Playwright, one project per application, against the built app      |
| `pnpm build`        | Regenerates the client, then builds both applications with Rolldown |

## Continuous integration

Everything above is checked by two commands, `dotnet test` and `pnpm check`. CI runs them on every
pull request and on every push to `main`, in one job per stack, so the two run in parallel and a red
build points at one side of the repository.

### The workflow

**`.github/workflows/ci.yml`**:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

# A new push to the same branch cancels the run that is now outdated.
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  backend:
    name: Backend
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json

      - run: dotnet restore

      # Warnings are errors and code style is enforced during the build
      # (TreatWarningsAsErrors, EnforceCodeStyleInBuild in Directory.Build.props),
      # so this is the lint step as well.
      - run: dotnet build --no-restore

      - run: dotnet test --no-build

      # The build writes frontend/openapi/AbsenceManagement.Api.json, and that file is
      # committed because the frontend generates its TypeScript client from it.
      - name: OpenAPI document is up to date
        run: |
          if [ -n "$(git status --porcelain -- frontend/openapi)" ]; then
            git status --short -- frontend/openapi
            echo "::error::The endpoints changed. Run 'dotnet build' and commit frontend/openapi."
            exit 1
          fi

  frontend:
    name: Frontend
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v5

      - uses: pnpm/action-setup@v4
        with:
          package_json_file: frontend/package.json

      - uses: actions/setup-node@v5
        with:
          node-version: 22
          cache: pnpm
          cache-dependency-path: frontend/pnpm-lock.yaml

      - run: pnpm install --frozen-lockfile

      # Type check, oxlint, the Nx boundary rule and the formatting check.
      - run: pnpm check

      # Vitest runs once rather than watching, because GitHub Actions sets CI=true.
      - run: pnpm test

      # Regenerates the client from the committed OpenAPI document, then builds both apps.
      - run: pnpm build

      - name: Generated API client is up to date
        run: |
          if [ -n "$(git status --porcelain -- packages/shared/api-client/src/generated)" ]; then
            git status --short -- packages/shared/api-client/src/generated
            echo "::error::The API client is stale. Run 'pnpm gen:api' and commit the result."
            exit 1
          fi

      - run: pnpm exec playwright install --with-deps

      # Playwright serves the built apps itself; the tests stub the API, so no backend is needed.
      - run: pnpm exec nx run-many -t e2e

      - if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: frontend/apps/*-e2e/test-output
          retention-days: 7
```

### Why it looks like this

**The backend job has no lint step.** `Directory.Build.props` sets `TreatWarningsAsErrors` and
`EnforceCodeStyleInBuild`, see [Add common files](#add-common-files), so an unused using or a
violated naming rule is a build error rather than a warning nobody reads. `dotnet build` is the
lint step, and there is no `dotnet format --verify-no-changes` next to it that could disagree
with it.

**Nothing has to be started.** The use case tests run against in-memory SQLite, and the Playwright
tests stub `/api` and drive the built application from `vite preview`. So there is no PostgreSQL
service container, no Docker, no `BASE_URL` and no secrets. The AppHost is compiled like every
other project but never run: it orchestrates local development, not CI.

**Two generated files are committed, so CI checks that they are current.**
`frontend/openapi/AbsenceManagement.Api.json` is written by `dotnet build`, and
`packages/shared/api-client/src/generated/` is written from it by `pnpm gen:api`, see
[The generated API client](#the-generated-api-client). Both are in Git, which is what lets the
frontend build without a .NET SDK — and what makes them go stale unnoticed when an endpoint
changes and only the C# side is committed. Each job regenerates its own file and fails if the
working tree moved. The check is `git status --porcelain` rather than `git diff --exit-code`, for
two reasons: `--exit-code` does not notice a generated file that is new and therefore still
untracked, and `.gitattributes` marks both paths `-diff`, so a diff would print
`Binary files differ` instead of naming the file that changed.

**`test` and `e2e`, not `test-ci` and `e2e-ci`.** The Nx Vitest and Playwright plugins register a
second, atomized target next to each of those, splitting a suite into one task per spec file. Those
targets refuse to start without Nx Cloud, so the workflow uses the plain ones, which are also what
`pnpm test` and `pnpm e2e` call. Vitest still runs once instead of watching: `testMode: "watch"` in
`nx.json` only sets the local default, and Vitest turns watching off whenever `CI` is set, which
GitHub Actions does for every step.

**pnpm comes from `pnpm/action-setup`, not from Corepack.** Locally it is enabled once with
`corepack enable`, see [Node and pnpm](#node-and-pnpm). In the workflow the order matters:
`actions/setup-node` can only fill its pnpm store cache if pnpm is already on the `PATH`, so the
pnpm action has to run before it. It reads the version from the `packageManager` field, which lives
in `frontend/package.json` and not at the repository root, hence `package_json_file`.

### What is configured on GitHub

A workflow reports, it does not block. The repository was created empty and with defaults, see
[Create the Git repository on GitHub](#create-the-git-repository-on-github); these settings are
added once CI has run for the first time.

**Ruleset on `main`** (Settings → Rules → Rulesets), targeting the default branch:

| Rule                                             | Value                    |
| ------------------------------------------------ | ------------------------ |
| Require a pull request before merging            | On                       |
| Require status checks to pass                    | `Backend` and `Frontend` |
| Require branches to be up to date before merging | On                       |
| Block force pushes                               | On                       |
| Restrict deletions                               | On                       |

The two status checks are the `name:` values of the jobs. GitHub only offers a check in that list
after it has reported at least once, so the first pull request is opened before the ruleset is
written.

**Actions permissions** (Settings → Actions → General):

| Setting                                                  | Value                    |
| -------------------------------------------------------- | ------------------------ |
| Workflow permissions                                     | Read repository contents |
| Allow GitHub Actions to create and approve pull requests | Off                      |

The workflow declares `permissions: contents: read` itself, so the repository default only has to
agree with it rather than grant more. If an organisation restricts actions to GitHub-owned and
verified creators, `pnpm/action-setup` has to be allowed, the four others are GitHub's own.

No secrets and no variables are needed, nothing in the workflow talks to anything outside the
repository. Cancelling superseded runs is not a repository setting either, the `concurrency` block
in the workflow does it. The one convenience worth turning on is **Automatically delete head
branches** under Settings → General → Pull Requests.
