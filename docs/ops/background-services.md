---
title: Background work
description: Hosted services, scheduled jobs, and queue consumers that survive restarts.
order: 30
---

## BackgroundService

```csharp
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
                await outbox.PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;                                  // shutting down: not an error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publishing failed; retrying on the next tick");
            }
        }
    }
}

builder.Services.AddHostedService<OutboxPublisher>();
```

Four things this gets right, and they are the four things usually got wrong:

1. **The loop never dies.** An unhandled exception in `ExecuteAsync` stops the service silently (and, if configured, takes the host down). Catch inside the loop.
2. **A scope per iteration.** The service is a singleton; `DbContext` is scoped. Never inject scoped services into the constructor.
3. **The token is honoured** everywhere, so shutdown is prompt.
4. **`PeriodicTimer`** instead of `Task.Delay` in a loop — no drift accumulation, no timer allocation per tick.

## Scheduling

`BackgroundService` gives you intervals, not schedules. For "every weekday at 06:00", either compute the next occurrence yourself or use a scheduler:

- **Quartz.NET** — cron expressions, persistent jobs, clustering.
- **Hangfire** — a durable job store plus a dashboard; good for fire-and-forget and retries.
- **The platform** — a Kubernetes CronJob or a cloud scheduler invoking an endpoint. Often the simplest correct answer, because it survives your process dying.

## Multiple instances

Two replicas run two copies of your background service. Decide explicitly:

- **Idempotent work** — let both run; design the work so doing it twice is harmless.
- **Single runner** — take a distributed lock (a row with `SELECT ... FOR UPDATE SKIP LOCKED`, Redis, or a lease) and let the loser idle.
- **Partitioned** — shard by tenant or by key so each instance owns a slice.

## Queue consumers

For work triggered by messages rather than time, consume from the broker (Azure Service Bus, RabbitMQ, Kafka, SQS) inside a hosted service, and:

- Acknowledge only after the work succeeded.
- Make handlers idempotent — every broker will deliver at least once eventually.
- Use a dead-letter queue with an alert; a silently discarded message is data loss.
- Bound concurrency, so a burst does not overwhelm the database.

## In-process queues

For work that may be lost on restart (sending a non-critical email, warming a cache), a bounded [`Channel<T>`](/docs/runtime/threading) with a `BackgroundService` consumer is enough — and honest about its durability. Anything that must not be lost belongs in a database or a broker before you return 200 to the caller.

## Further reading

- [Background tasks with hosted services](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services)
