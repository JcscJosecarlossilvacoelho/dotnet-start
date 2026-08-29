---
title: Glossary
description: The vocabulary of .NET, defined once so the rest of these pages can use it.
order: 50
---

**AOT (ahead-of-time)** — compiling IL to machine code before running. See [Native AOT](/docs/runtime/native-aot).

**Assembly** — the unit of deployment and versioning: a `.dll` or `.exe` containing IL, metadata, and resources.

**BCL (Base Class Library)** — the `System.*` types that ship with the runtime.

**Blazor** — the .NET component model for UI. See [Blazor](/docs/ui/blazor).

**BackgroundService** — a hosted service with a long-running `ExecuteAsync` loop. See [Background work](/docs/ops/background-services).

**Captive dependency** — a scoped service held by a singleton, effectively promoting its lifetime. See [DI](/docs/web/dependency-injection).

**Circuit** — the SignalR connection backing an Interactive Server Blazor session. See [render modes](/docs/ui/render-modes).

**CLR / CoreCLR** — the runtime: JIT compilation, garbage collection, type loading, exceptions.

**DbContext** — EF Core's unit of work and change tracker. See [EF Core essentials](/docs/data/ef-core).

**Endpoint** — a route plus the delegate that handles it, along with its metadata. See [Routing](/docs/web/routing).

**Generic Host** — the object owning configuration, logging, DI, and lifetime. Built by `WebApplication.CreateBuilder`.

**Hosted service** — a background component started and stopped with the host (`IHostedService`).

**IL (Intermediate Language)** — the bytecode C# compiles to, JIT-compiled at runtime unless published AOT.

**JIT** — the just-in-time compiler that turns IL into machine code, tiering up hot methods.

**Kestrel** — the cross-platform HTTP server ASP.NET Core runs on.

**LOH (Large Object Heap)** — the heap segment for objects ≥ 85,000 bytes. See [GC](/docs/runtime/garbage-collection).

**LTS / STS** — long-term (3 years) and standard-term (18 months) support releases. See [Versions](/docs/reference/versions-and-support).

**Middleware** — a component in the request pipeline. See [Middleware](/docs/web/middleware).

**Minimal API** — mapping a route directly to a delegate. See [Minimal APIs](/docs/web/minimal-apis).

**MSBuild** — the build engine that reads `.csproj` files. See [Project files](/docs/start/project-files).

**NuGet** — the package manager and package format. See [NuGet](/docs/start/nuget).

**Outbox** — writing an event in the same transaction as the state change, publishing it afterwards. See [Messaging](/docs/architecture/messaging).

**ProblemDetails** — the RFC 9457 error response format. See [Error handling](/docs/web/error-handling).

**Razor** — the syntax mixing markup with C# in `.razor` and `.cshtml` files.

**RID (Runtime Identifier)** — a platform target like `linux-x64` or `osx-arm64`, used when publishing.

**Roslyn** — the C# and VB compiler platform, and the host for analysers and [source generators](/docs/csharp/source-generators).

**Scoped / Singleton / Transient** — the three DI lifetimes. See [DI](/docs/web/dependency-injection).

**SDK** — the compilers, MSBuild, templates, and CLI. See [The SDK and the CLI](/docs/start/sdk-and-cli).

**Span&lt;T&gt;** — a stack-only view over contiguous memory. See [Span and Memory](/docs/csharp/spans-and-memory).

**TFM (Target Framework Moniker)** — `net10.0` and friends. See [Versions](/docs/reference/versions-and-support).

**Thread pool** — the shared pool that runs most .NET work. See [Threads and tasks](/docs/runtime/threading).

**Trimming** — removing unreferenced IL when publishing, to shrink the output.

**WebApplicationFactory** — the test host that runs the real application in memory. See [Integration testing](/docs/testing/integration-testing).
