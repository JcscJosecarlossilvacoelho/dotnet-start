---
title: What .NET is
description: The runtime, the libraries, the SDK, and the languages — and how the pieces relate to each other.
order: 5
---

.NET is four things that ship together and are often confused with one another: a **runtime**, a **standard library**, an **SDK**, and a set of **languages**. Knowing which one you are talking about removes most of the confusion around versions, deployment, and compatibility.

## The four pieces

| Piece | What it is | Where you meet it |
| --- | --- | --- |
| Runtime (CoreCLR) | Executes IL, manages memory, does JIT compilation | `dotnet run`, production servers |
| Base Class Library | `System.*` — collections, IO, networking, text, time | Every `using System...` |
| SDK | Compilers, MSBuild, templates, the `dotnet` CLI | Your machine, CI |
| Languages | C#, F#, Visual Basic | Your source files |

A machine that only *runs* applications needs the runtime. A machine that *builds* them needs the SDK, which contains a runtime of its own.

## From source to a running process

1. **Restore** — MSBuild reads the project file and resolves the NuGet graph into `obj/project.assets.json`.
2. **Compile** — Roslyn compiles C# into IL inside a managed assembly (`.dll`), plus a portable PDB.
3. **Publish** — the assembly, its dependencies, and a runtime configuration file are laid out in a folder.
4. **Run** — the host (`dotnet` or the app's own executable) starts the runtime, which JIT-compiles IL to machine code on first execution of each method.

Nothing in this pipeline is hidden from you. `dotnet build -v n` shows every target that runs; `obj/` holds the generated files.

## One .NET, many workloads

Since .NET 5 there is a single product line. The same runtime and BCL power:

- **ASP.NET Core** — web APIs, real-time apps, and server-rendered UI.
- **Blazor** — component UI running on the server or in WebAssembly.
- **.NET MAUI** — iOS, Android, macOS, and Windows applications.
- **Worker Services** — long-running background processes and message consumers.
- **Console tools** — CLIs, one-off scripts, source generators.

There is no ".NET Framework vs .NET Core" decision for new work. Build on the current .NET release; .NET Framework 4.8 is a Windows-only, maintenance-mode product.

## Managed execution, briefly

Your code runs under a managed runtime, which means three guarantees that shape how you write C#:

- **Memory is reclaimed by a garbage collector.** You allocate; you rarely free. See [Garbage collection](/docs/runtime/garbage-collection).
- **Types are verified.** The runtime knows the layout of every object, enabling reflection, serialization, and safe casting.
- **Code is compiled at runtime by default.** The JIT can specialise for the actual CPU. [Native AOT](/docs/runtime/native-aot) trades that away for instant startup.

## Release cadence

A new major version ships every November. Even-numbered releases are **LTS** (3 years of support); odd-numbered releases are **STS** (18 months). Upgrading is usually a one-line change to `<TargetFramework>` — see [Versions and support](/docs/reference/versions-and-support).

## Further reading

- [.NET architectural overview](https://learn.microsoft.com/dotnet/core/introduction)
- [.NET release policies](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
