---
title: Project files and MSBuild
description: How a .csproj describes a build, the properties worth setting, and how Directory.Build.props keeps a repository consistent.
order: 50
---

A `.csproj` is an MSBuild file: XML describing **properties** (scalar settings) and **items** (files and references). The SDK supplies the targets; your project supplies the differences from the default.

## A minimal project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

No file list appears: the SDK globs `**/*.cs` by default. You only declare files when you need to exclude, add, or configure them.

## Properties worth setting deliberately

| Property | Effect |
| --- | --- |
| `TargetFramework` | The API surface and runtime you compile against (`net10.0`) |
| `Nullable` | Enables nullable reference type analysis — keep it `enable` |
| `ImplicitUsings` | Adds a curated set of `global using` directives |
| `TreatWarningsAsErrors` | Stops warnings from accumulating |
| `LangVersion` | Pins the C# version; usually leave it to the SDK |
| `InvariantGlobalization` | Drops ICU; smaller images, but culture-sensitive behaviour changes |
| `PublishAot` | Compiles ahead of time to a native binary |
| `GenerateDocumentationFile` | Emits XML docs and enables missing-doc warnings |

## Items: references, packages, content

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  <ProjectReference Include="../MyApp.Domain/MyApp.Domain.csproj" />
  <Content Include="docs/**/*.md" CopyToOutputDirectory="PreserveNewest" />
  <Compile Remove="Generated/**" />
</ItemGroup>
```

`PackageReference` is transitive: consumers of your library get your dependencies. `PrivateAssets="all"` stops that when a package is a build-time-only concern (analysers, source generators).

## Repository-wide settings

`Directory.Build.props` at the repository root is imported by every project below it, before the project's own content. It is where shared settings belong:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

`Directory.Build.targets` does the same but is imported *after* the project, which is where you override things the project set.

## Central Package Management

With `Directory.Packages.props`, versions live in one file and projects reference package names only:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  </ItemGroup>
</Project>
```

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

This removes the most common source of drift in multi-project repositories.

## Reading the build

- `dotnet build -v n` — which targets ran, in order.
- `dotnet msbuild -pp:full.xml` — the fully evaluated project, imports included.
- `dotnet build -bl` then open `msbuild.binlog` in the MSBuild Structured Log Viewer.

## Further reading

- [MSBuild reference](https://learn.microsoft.com/visualstudio/msbuild/msbuild-reference)
- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
