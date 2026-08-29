---
title: Solutions and repository layout
description: How to organise projects, and how many projects you actually need.
order: 70
---

Most teams create too many projects too early. A project boundary costs build time and indirection; it buys compile-time enforcement of a dependency rule. Create one only when you want that rule enforced.

## A layout that scales down and up

```
src/
  MyApp.Api/           # host: endpoints, DI wiring, configuration
  MyApp.Domain/        # entities, value objects, business rules — no framework references
  MyApp.Infrastructure/# EF Core, HTTP clients, message bus
tests/
  MyApp.UnitTests/
  MyApp.IntegrationTests/
docs/
MyApp.slnx
Directory.Build.props
Directory.Packages.props
global.json
```

The rule this enforces: `Domain` references nothing, `Infrastructure` references `Domain`, `Api` references both. If `Domain` cannot compile against EF Core, business rules cannot quietly depend on the database.

For a service under a few thousand lines, a single project with folders is the better trade. Split when a rule is being broken repeatedly, not in anticipation.

## Solution files

`.slnx` is the modern XML solution format — readable and merge-friendly:

```bash
dotnet new sln --format slnx -n MyApp
dotnet sln add src/**/*.csproj tests/**/*.csproj
dotnet sln list
```

The solution exists for tooling (build order, `dotnet test` discovery). It carries no runtime meaning.

## Files every repository should have

| File | Why |
| --- | --- |
| `global.json` | Pins the SDK so CI and laptops agree |
| `Directory.Build.props` | One place for shared compiler settings |
| `Directory.Packages.props` | One version per package |
| `.editorconfig` | Formatting and analyser severity, enforced by `dotnet format` |
| `.gitignore` | `dotnet new gitignore` generates a correct one |
| `nuget.config` | Explicit, minimal package sources |

## Namespaces and folders

Keep namespaces aligned with folders (`RootNamespace` + relative path) and use file-scoped namespace declarations. Predictable placement is worth more than a clever scheme, both to newcomers and to coding agents.

## Further reading

- [.slnx solution format](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln)
