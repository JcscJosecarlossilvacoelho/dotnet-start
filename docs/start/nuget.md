---
title: NuGet and dependencies
description: How packages resolve, how to keep the graph honest, and how to publish your own.
order: 60
---

NuGet is the package manager and the format. Understanding resolution saves hours of confusing build failures.

## How a version is chosen

- A version string is a **minimum**, not an exact pin. `Version="10.0.0"` means "10.0.0 or the lowest available above it".
- **Nearest wins**: a direct reference always beats a transitive one, regardless of version.
- **Lowest applicable wins** for transitive conflicts: NuGet picks the lowest version satisfying every constraint.
- Ranges are supported: `[10.0.0]` (exact), `[10.0.0,11.0.0)` (half-open).

Because versions are minimums, two machines can restore different graphs. A lock file removes that variance:

```bash
dotnet restore --use-lock-file
dotnet restore --locked-mode   # in CI: fail if the lock file would change
```

## Inspecting the graph

```bash
dotnet list package --include-transitive
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive
dotnet nuget why MyApp.csproj System.Text.Json
```

Run `--vulnerable` in CI and fail the build on findings. It is the cheapest supply-chain control available.

## Restore sources

`nuget.config` beside the solution controls where packages come from. Clear inherited sources explicitly so a machine-level source cannot silently supply a package:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

For private feeds, add `packageSourceMapping` so each package prefix resolves from exactly one source — this defeats dependency-confusion attacks.

## Publishing a package

```xml
<PropertyGroup>
  <PackageId>Contoso.Widgets</PackageId>
  <Version>1.2.0</Version>
  <Authors>Contoso</Authors>
  <Description>Widget primitives for Contoso services.</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <RepositoryUrl>https://github.com/contoso/widgets</RepositoryUrl>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

```bash
dotnet pack -c Release
dotnet nuget push bin/Release/Contoso.Widgets.1.2.0.nupkg --source nuget.org --api-key $NUGET_KEY
```

Set `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>` in CI to make builds deterministic and source-linkable.

## Versioning rules of thumb

Follow SemVer, and remember that for a library, the *binary* surface is the contract: adding a parameter with a default value, reordering enum members, or changing a struct's layout are breaking changes even when the source still compiles.

## Further reading

- [NuGet dependency resolution](https://learn.microsoft.com/nuget/concepts/dependency-resolution)
- [Package source mapping](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping)
