---
title: Deployment
description: Publish modes, runtime identifiers, environments, and rolling out without breaking live traffic.
order: 20
---

## Publish modes

```bash
dotnet publish -c Release                                  # framework-dependent
dotnet publish -c Release -r linux-x64 --self-contained    # bundles the runtime
dotnet publish -c Release -r linux-x64 -p:PublishAot=true  # native binary
```

| Mode | Needs a runtime installed | Size | Startup |
| --- | --- | --- | --- |
| Framework-dependent | Yes | Smallest output | Normal |
| Self-contained | No | ~70 MB+ | Normal |
| [Native AOT](/docs/runtime/native-aot) | No | Small binary | Fastest |

Framework-dependent inside a runtime image is the usual container answer; self-contained suits machines you do not control.

## Environments

`ASPNETCORE_ENVIRONMENT` selects the configuration overlay and the behaviour of developer-only middleware. Keep the set small — `Development`, `Staging`, `Production` — and make staging identical to production in everything except data.

## Rolling out

A rolling deployment runs old and new versions simultaneously. Everything you ship must tolerate that:

- **Schema** — expand/contract only, never a breaking change in one step. See [Migrations](/docs/data/migrations).
- **Contracts** — additive changes to APIs and message payloads; version when you must remove.
- **Configuration** — the new version must start with the configuration the old one had, plus defaults.

Ordering that works: migrate the schema (compatible with both), deploy the new code, then remove the old schema in a later release.

## Health probes drive the rollout

The orchestrator needs to know when the new instance is ready and when an old one has drained:

```csharp
app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
```

See [Health checks](/docs/ops/health-checks).

## Graceful shutdown

On SIGTERM the host stops accepting new requests and waits for in-flight work. Two failure modes to avoid: a shutdown timeout shorter than your slowest request (truncated responses), and background services that ignore the stopping token (a hung pod that the orchestrator eventually kills).

## Rollback

Every deployment needs an answer to "how do we undo this in one minute". Keep the previous image, keep migrations backwards-compatible for at least one release, and use feature flags so a behaviour change can be reverted without a redeploy.

## A CI pipeline that earns its keep

```yaml
- run: dotnet restore --locked-mode
- run: dotnet build -c Release --no-restore
- run: dotnet test -c Release --no-build --logger trx
- run: dotnet list package --vulnerable --include-transitive
- run: dotnet publish -c Release /t:PublishContainer
```

Fail on warnings, fail on vulnerable packages, and publish the exact artifact you tested.

## Further reading

- [.NET application publishing](https://learn.microsoft.com/dotnet/core/deploying/)
