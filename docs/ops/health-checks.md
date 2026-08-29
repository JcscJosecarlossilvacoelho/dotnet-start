---
title: Health checks
description: Liveness, readiness, and startup probes that tell the truth.
order: 40
---

A health check exists to answer one question for one consumer: **should traffic be sent here, and should this process be restarted?**

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"])
    .AddUrlGroup(new Uri("https://payments.example.com/health"), "payments", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
```

## The three probes

| Probe | Question | Failure action | Should check |
| --- | --- | --- | --- |
| Liveness | Is the process wedged? | Restart the container | Nothing external — only the process itself |
| Readiness | Can it serve traffic now? | Remove from the load balancer | Dependencies it cannot work without |
| Startup | Has it finished initialising? | Keep waiting | Warm-up, migrations, cache priming |

The most common and most damaging mistake is checking the database in the **liveness** probe. The database blips, every instance reports unhealthy, the orchestrator restarts all of them at once, and a brief dependency problem becomes a full outage.

## Writing a check

```csharp
public sealed class QueueDepthCheck(IQueueClient queue) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var depth = await queue.GetDepthAsync(cancellationToken);

        return depth switch
        {
            < 1_000  => HealthCheckResult.Healthy($"depth {depth}"),
            < 10_000 => HealthCheckResult.Degraded($"depth {depth}"),
            _        => HealthCheckResult.Unhealthy($"depth {depth}")
        };
    }
}
```

Checks must be **fast** (probes run every few seconds), **cheap** (no `SELECT COUNT(*)` on a big table), and **timeout-bounded**. Cache expensive results for a few seconds rather than hammering a dependency from every replica.

## Degraded is useful

`Degraded` means "still serving, but something is wrong". Keep it out of the readiness predicate and route it to your alerting instead — it is the signal that lets you fix a problem before it becomes an incident.

## Exposing details

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();
```

A detailed body is helpful internally and an information leak externally. Keep detailed health on an internal port or behind authorization; keep the public endpoint to a status code.

## Further reading

- [Health checks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
