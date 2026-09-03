---
name: dotnet-observability
description: Make a .NET service observable and resilient in production. Use for structured logging, OpenTelemetry traces and metrics, health checks, correlation, retries and timeouts with Polly/resilience handlers, and diagnosing a live process.
---

# Observability and resilience

An endpoint is not finished when it returns the right body. It is finished when a failure at 3am can be found from the outside.

Read what the service already emits before adding anything: the logging configuration, whether OpenTelemetry or Application Insights is wired up, and whether the app is orchestrated by .NET Aspire (which configures most of this for you through a `ServiceDefaults` project — extend that, do not duplicate it).

## Log for machines, not for eyes

```csharp
// Good — one event, queryable fields, no string building.
logger.LogInformation("Order {OrderId} shipped to {Region} in {Elapsed}ms", order.Id, region, elapsed);

// Avoid — unqueryable, allocates even when Information is disabled.
logger.LogInformation("Order " + order.Id + " shipped to " + region);
```

- Use the message template. Placeholders become structured fields; interpolation destroys them.
- Prefer compile-time logging source generators on hot paths:
  ```csharp
  [LoggerMessage(Level = LogLevel.Warning, Message = "Payment {PaymentId} retried {Attempt} times")]
  static partial void PaymentRetried(ILogger logger, string paymentId, int attempt);
  ```
- Levels mean something: `Error` = a human must look; `Warning` = degraded but handled; `Information` = a business-meaningful event; `Debug`/`Trace` = off in production.
- Log an exception as the first argument (`logger.LogError(ex, "...")`), never `ex.Message` in the template — you lose the stack trace.
- Never log secrets, tokens, full request bodies, or personal data. Redact at the point of logging.
- Log a failure once, where it is handled. Log-and-rethrow at every layer produces the same incident five times.
- Use `logger.BeginScope` for ambient identifiers (tenant, correlation id) instead of repeating them in every message.

## OpenTelemetry is the default

```csharp
builder.Logging.AddOpenTelemetry(o => { o.IncludeFormattedMessage = true; o.IncludeScopes = true; });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("orders-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .UseOtlpExporter();
```

- One `ActivitySource` and one `Meter` per component, held as `static readonly`, named after the assembly. Register the name with the provider or nothing is exported.
- Add custom spans for meaningful work, not for every method. Put the identifying data on the span as tags — a span with no tags cannot answer a question.
- Prefer metrics over logs for anything you would count or graph: `Counter<T>` for events, `Histogram<T>` for durations and sizes, an observable gauge for queue depth. Keep tag cardinality low — never a user id or an order id as a tag.
- Sample traces in production. Use parent-based head sampling when a representative subset is enough; use a collector with tail sampling when the decision must depend on the completed trace, such as retaining every error or high-latency trace.

## Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"])
    .AddUrlGroup(new Uri(paymentsUrl), name: "payments", tags: ["ready"]);

app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
```

Liveness must not touch dependencies — a failing database should not cause the orchestrator to kill a healthy process. Readiness may. Keep both cheap and give them a timeout.

## Resilience

Use `Microsoft.Extensions.Http.Resilience` on typed clients rather than hand-rolled retry loops:

```csharp
builder.Services.AddHttpClient<PaymentsClient>(c => c.BaseAddress = new Uri(paymentsUrl))
    .AddStandardResilienceHandler();   // timeout + retry + circuit breaker + rate limiter
```

- Every outbound call needs a timeout. A retry without a timeout multiplies an outage.
- Retry only idempotent operations, with exponential backoff **and jitter**. Send an idempotency key when retrying a write.
- Add a circuit breaker for a dependency that can be down; add a fallback only when a degraded answer is genuinely better than an error.
- Never retry a `4xx` other than `408`/`429`, and honour `Retry-After`.
- Shut down gracefully: honour the stopping token, drain in-flight requests, and set the container's termination grace period above your drain time.

## Diagnose a running process

```bash
dotnet-counters monitor -n <process> System.Runtime Microsoft.AspNetCore.Hosting
dotnet-trace collect -n <process> --profile cpu-sampling
dotnet-gcdump collect -n <process>     # suspected leak: compare two dumps
dotnet-dump collect -n <process>
```

## Complete the change

Run the service and prove the signal exists: hit the endpoint, then show the log line, the span, or the metric that appeared. State explicitly which failure modes are now visible and which are still silent.
