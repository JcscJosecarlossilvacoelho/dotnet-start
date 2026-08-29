---
title: Source generators and analysers
description: Compile-time code generation and custom rules — what they are for and how to consume them.
order: 90
---

Roslyn exposes the compiler as a library. Two capabilities matter to application developers: **analysers** report problems as you type, and **source generators** add code to the compilation before it is emitted.

## Generators you are probably already using

| Generator | What it removes |
| --- | --- |
| `System.Text.Json` source generation | Reflection-based serialization; enables trimming and AOT |
| `LoggerMessage` | Boxing and string formatting on every log call |
| `[GeneratedRegex]` | Regex interpretation at runtime |
| Minimal API / MVC request delegate generator | Runtime reflection over endpoint signatures |
| `[LibraryImport]` | Hand-written P/Invoke marshalling |

```csharp
[JsonSerializable(typeof(Order))]
internal sealed partial class AppJsonContext : JsonSerializerContext;

[LoggerMessage(Level = LogLevel.Warning, Message = "Order {OrderId} was rejected: {Reason}")]
public static partial void OrderRejected(ILogger logger, Guid orderId, string reason);

[GeneratedRegex(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$", RegexOptions.IgnoreCase)]
private static partial Regex EmailPattern();
```

Each of these is faster *and* trim-safe, which is why they are the recommended form rather than a micro-optimisation.

## Consuming a generator

A generator is a normal NuGet package, referenced with `PrivateAssets` so it never flows to consumers:

```xml
<PackageReference Include="Some.Generator" Version="1.0.0"
                  PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

To read what was generated:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>
```

## Analysers as project policy

`.editorconfig` sets severity per rule, and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` makes the build enforce it:

```ini
[*.cs]
dotnet_diagnostic.CA2007.severity = none      # ConfigureAwait, irrelevant in ASP.NET Core
dotnet_diagnostic.CA1848.severity = warning   # use LoggerMessage delegates
csharp_style_namespace_declarations = file_scoped:warning
```

Enable the built-in sets deliberately: `<AnalysisLevel>latest-recommended</AnalysisLevel>`, plus `<EnableNETAnalyzers>true</EnableNETAnalyzers>`.

## Writing your own

Implement `IIncrementalGenerator`. The word *incremental* is the whole design: build a pipeline of small, cacheable steps so the IDE does not re-run your work on every keystroke. Never do IO in a generator, and never depend on files outside the compilation except through `AdditionalFiles`.

## Further reading

- [Source generators](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Code analysis configuration](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options)
