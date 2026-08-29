---
title: Calling other services
description: HttpClient without socket exhaustion — typed clients, resilience, and the settings that matter.
order: 110
---

`HttpClient` is thread-safe and expensive to create. Creating one per request exhausts sockets; creating one static instance never notices DNS changes. `IHttpClientFactory` solves both by pooling handlers with a rotation lifetime.

## Typed clients

```csharp
builder.Services.AddHttpClient<PaymentClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Payments:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
});

public sealed class PaymentClient(HttpClient http)
{
    public async Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("/v1/charges", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentResult>(ct))!;
    }
}
```

The client is registered as transient over a pooled handler: inject it anywhere, including scoped services.

## Resilience

```csharp
builder.Services.AddHttpClient<PaymentClient>()
    .AddStandardResilienceHandler();
```

`Microsoft.Extensions.Http.Resilience` adds, in order: a rate limiter, a total timeout, retries with exponential backoff and jitter, a circuit breaker, and a per-attempt timeout. Tune it rather than hand-rolling:

```csharp
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.CircuitBreaker.FailureRatio = 0.2;
});
```

**Only retry idempotent operations.** For a POST that creates something, send an idempotency key and let the server deduplicate.

## Cancellation and timeouts

Pass the request's `CancellationToken` everywhere. Client timeout, resilience timeout, and caller cancellation should all collapse into one token so a dead request stops consuming resources end to end.

## Streaming large responses

```csharp
using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
await using var stream = await response.Content.ReadAsStreamAsync(ct);
await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<Item>(stream, cancellationToken: ct))
    ...
```

`ResponseHeadersRead` avoids buffering the whole body in memory.

## Instrumentation

The factory logs every request and emits metrics automatically; adding the OpenTelemetry HTTP instrumentation propagates the trace context to the downstream service, so one trace spans both. See [Observability](/docs/ops/observability).

## Further reading

- [IHttpClientFactory](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
- [Building resilient HTTP apps](https://learn.microsoft.com/dotnet/core/resilience/http-resilience)
