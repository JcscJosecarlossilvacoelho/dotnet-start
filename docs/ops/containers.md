---
title: Containers
description: Building small, correct .NET images — multi-stage builds, base image choices, and container-aware runtime settings.
order: 10
---

## A multi-stage Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so restore is cached independently of source changes.
COPY *.sln Directory.*.props ./
COPY src/MyApp.Api/*.csproj src/MyApp.Api/
RUN dotnet restore src/MyApp.Api/MyApp.Api.csproj

COPY . .
RUN dotnet publish src/MyApp.Api/MyApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
```

Restoring before copying the full source is the difference between a 10-second and a 3-minute rebuild.

## Choosing a base image

| Tag | Size | Notes |
| --- | --- | --- |
| `aspnet:10.0` | Largest | Debian; a shell and package manager for debugging |
| `aspnet:10.0-alpine` | Smaller | musl libc; verify native dependencies |
| `aspnet:10.0-noble-chiseled` | Small | No shell, no package manager — a much smaller attack surface |
| `runtime-deps` + [Native AOT](/docs/runtime/native-aot) | Smallest | Self-contained native binary |

Chiseled images are an excellent production default. Debug them with an ephemeral sidecar rather than by adding a shell back.

## Building without a Dockerfile

```bash
dotnet publish -c Release /t:PublishContainer -p:ContainerRepository=myapp
```

The SDK builds an OCI image directly, with sensible defaults and layer caching. For simple services it removes the Dockerfile entirely.

## Runtime settings that matter in a container

```dockerfile
ENV DOTNET_gcServer=0                    # small containers often do better with workstation GC
ENV DOTNET_GCHeapHardLimitPercent=75     # bound the heap to the container limit
ENV ASPNETCORE_HTTP_PORTS=8080           # non-privileged port; run as non-root
ENV DOTNET_EnableDiagnostics=0           # disable the diagnostic socket in production
```

.NET reads cgroup limits, so a memory limit on the container does bound the GC — but only if the limit is actually set. An unlimited container plus server GC is how a service gets OOM-killed at 2 a.m.

## Health and shutdown

Expose [health endpoints](/docs/ops/health-checks) for the orchestrator's liveness and readiness probes, and handle SIGTERM: the host stops accepting connections, drains in-flight requests, and then exits. Give it time:

```csharp
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
```

## Image hygiene

- Never bake secrets into an image — they live in every layer forever.
- Pin base image digests for reproducibility, and rebuild regularly for security updates.
- Scan images in CI (`trivy`, `docker scout`) alongside `dotnet list package --vulnerable`.
- One process per container; let the orchestrator supervise.

## Further reading

- [.NET container images](https://learn.microsoft.com/dotnet/core/docker/introduction)
