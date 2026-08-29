---
title: Resilience
description: Timeouts, retries, circuit breakers, and the failure modes they cause when configured badly.
order: 70
---

Everything remote fails. Resilience is deciding in advance what happens when it does.

## The policies

| Policy | Protects against | Danger |
| --- | --- | --- |
| Timeout | Hanging calls holding resources | Too short: false failures |
| Retry | Transient blips | Retry storms; duplicate side effects |
| Circuit breaker | Hammering a service that is down | Opening on normal error rates |
| Bulkhead / concurrency limit | One dependency starving the process | Rejecting work you could have served |
| Fallback | Total unavailability | Serving wrong data silently |
| Hedging | Tail latency | Doubling load on a struggling service |

## Configuration with Polly

```csharp
builder.Services.AddResiliencePipeline("payments", pipeline =>
{
    pipeline
        .AddTimeout(TimeSpan.FromSeconds(10))                      // total
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,                                       // essential: prevents synchronised retries
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.2,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 20,
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddTimeout(TimeSpan.FromSeconds(3));                      // per attempt
});
```

For HTTP clients, `AddStandardResilienceHandler()` composes exactly this shape with sensible defaults — see [Calling other services](/docs/web/http-client).

## Rules

**Set a timeout on everything.** No timeout means a thread and a connection are held until the OS gives up, which is how one slow dependency exhausts a whole service.

**Only retry idempotent operations.** GETs and PUTs are usually safe; POSTs are not, unless the server deduplicates on an idempotency key. Retrying a charge without one charges twice.

**Always jitter.** Synchronised exponential backoff across a thousand clients is a coordinated attack on a recovering service.

**Bound total time.** Three retries with a 10-second timeout each is a 30-second request. The caller's own timeout probably fired at 5 — you did the work for nobody.

**Fail fast at capacity.** Under overload, [rate limiting](/docs/web/rate-limiting) and load shedding beat queueing.

## Degrade deliberately

Decide per feature what "degraded" means: a cached price, a placeholder recommendation, a queued write. Serving something stale and saying so is usually better than a 500 — but only when the user can tell. Never silently return an empty list where an error occurred; that reads as "no orders" and generates a support ticket you cannot reproduce.

## Verify it

Resilience code that has never failed in a test is a guess. Force it: point a test at a dead port, add latency with a proxy, kill a container mid-request. What you learn is usually that a timeout somewhere is wrong.

## Further reading

- [Resilience in .NET](https://learn.microsoft.com/dotnet/core/resilience/)
- [Polly](https://www.pollydocs.org/)
