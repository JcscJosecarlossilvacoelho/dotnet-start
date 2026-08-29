---
title: The SDK and the CLI
description: What `dotnet` does, which commands matter, and how SDK versions are selected per repository.
order: 40
---

The `dotnet` CLI is the same tool your IDE and your CI pipeline use. Everything an IDE can do to a project, the CLI can do, and the CLI is the definition of correct behaviour.

## Commands that matter

| Command | What it does |
| --- | --- |
| `dotnet new <template>` | Scaffolds a project, file, or configuration from a template |
| `dotnet restore` | Resolves the NuGet graph (implicit in build/run/test) |
| `dotnet build` | Compiles the project and its references |
| `dotnet run` | Builds and starts the project (`--no-build` to skip) |
| `dotnet watch` | Rebuilds and hot-reloads on file change |
| `dotnet test` | Runs the test projects in the solution |
| `dotnet publish` | Produces a deployable folder |
| `dotnet format` | Applies `.editorconfig` formatting and style fixes |
| `dotnet list package --vulnerable` | Reports known-vulnerable dependencies |
| `dotnet nuget why <pkg>` | Explains why a transitive package is in the graph |

Add `-v n` (normal verbosity) to any build command when a failure is unclear, and `--property:Key=Value` to override any MSBuild property from the command line.

## Pinning the SDK per repository

A `global.json` at the repository root fixes which SDK builds the code, so that a developer with a newer preview installed does not produce different output from CI:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

`rollForward` controls how far the resolver may move: `patch` (safest), `latestFeature` (a good default), `latestMajor` (loosest). Without `global.json`, the newest installed SDK wins.

## Tools

**Global tools** are installed once per machine, **local tools** are pinned per repository in `.config/dotnet-tools.json` and restored on demand. Prefer local tools — they are versioned with the code.

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
dotnet tool restore          # on a fresh clone or in CI
dotnet ef migrations add Initial
```

## Templates

`dotnet new list` shows what is installed. The templates worth knowing:

- `webapi` — HTTP API with OpenAPI configured.
- `blazor` — Blazor Web App with selectable render modes.
- `worker` — a hosted background service.
- `classlib` — a reusable library.
- `xunit` / `nunit` / `mstest` — test projects.
- `gitignore`, `editorconfig`, `globaljson`, `nugetconfig` — configuration files, not projects.

## Diagnosing a broken build

1. `dotnet --info` — is the expected SDK being selected?
2. `dotnet restore --force --no-cache` — is it a stale package cache?
3. `dotnet build -v n` — which target actually failed?
4. `rm -rf bin obj` — is it a stale intermediate output?

## Further reading

- [dotnet CLI reference](https://learn.microsoft.com/dotnet/core/tools/)
- [global.json reference](https://learn.microsoft.com/dotnet/core/tools/global-json)
