---
title: Rate limiting and resilience
description: Protecting a service from load it cannot serve — limiter algorithms, partitioning, and graceful degradation.
order: 150
---

Rate limiting is capacity protection, not security. It keeps one caller from consuming the throughput everyone else needs, and it turns an outage into a set of 429s.

## The built-in limiters

| Limiter | Behaviour | Good for |
| --- | --- | --- |
| Fixed window | N per interval, resets on the boundary | Simple quotas |
| Sliding window | N per rolling interval | Smoother than fixed; avoids boundary bursts |
| Token bucket | Refills at a steady rate, allows bursts | Public APIs |
| Concurrency | N simultaneous requests | Protecting an expensive dependency |

## Configuring

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("per-user", context => RateLimitPartition.GetTokenBucketLimiter(
        partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.Connection.RemoteIpAddress?.ToString()
                      ?? "anonymous",
        factory: _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 100,
            TokensPerPeriod = 20,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            QueueLimit = 0
        }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "10";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = 429, Title = "Too many requests" }, ct);
    };
});

app.UseRateLimiter();
app.MapGet("/search", Search).RequireRateLimiting("per-user");
```

Always send `Retry-After`. A client that does not know when to come back will hammer you.

## Partitioning is the design decision

Limit per **user**, per **API key**, or per **tenant** — not per IP, unless you have no identity. Behind a proxy, an IP-based limit can throttle an entire office or, worse, be trivially bypassed with spoofed forwarded headers.

## Where limiting is not the answer

- **Backpressure inside the process** — use a bounded [`Channel<T>`](/docs/runtime/threading).
- **Protecting a downstream service you call** — use a [resilience pipeline](/docs/web/http-client) with a circuit breaker.
- **Denial of service at the edge** — that belongs in your CDN or WAF; application-level limiting still has to accept the connection.

## Degrading gracefully

Under overload, shedding load fast is better than queueing. Keep queue limits small or zero, return 429/503 quickly, and keep health endpoints outside the limiter so orchestrators can still see the process.

Report what you shed: a metric for rejected requests, partitioned by policy, tells you whether the limit is protecting the service or strangling a legitimate customer.

## Further reading

- [Rate limiting middleware](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
