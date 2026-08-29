---
title: Observability
description: Traces, metrics, and logs with OpenTelemetry — instrumenting a service so production questions have answers.
order: 50
---

Three signals answer three different questions: **metrics** say something is wrong, **traces** say where, **logs** say why. .NET emits all three natively; OpenTelemetry exports them anywhere.

## Wiring it up

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders-api", serviceVersion: "1.4.2"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MyApp"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApp"))
    .UseOtlpExporter();
```

One environment variable (`OTEL_EXPORTER_OTLP_ENDPOINT`) then points the whole thing at a collector, Jaeger, Grafana, Honeycomb, or a vendor.

## Custom spans

```csharp
private static readonly ActivitySource Source = new("MyApp");

using var activity = Source.StartActivity("charge-payment");
activity?.SetTag("order.id", order.Id);
activity?.SetTag("payment.provider", "stripe");

try
{
    await gateway.ChargeAsync(order, ct);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

Add a span for anything that can be slow and is not already instrumented. Add tags with the identifiers you would want when searching for one bad request among millions — and never put personal data or secrets in a tag.

## Custom metrics

```csharp
private static readonly Meter Meter = new("MyApp");
private static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("orders.placed");
private static readonly Histogram<double> ChargeDuration = Meter.CreateHistogram<double>("payment.charge.duration", "ms");

OrdersPlaced.Add(1, new KeyValuePair<string, object?>("channel", channel));
```

Keep cardinality low: a tag whose values are user ids will destroy your metrics backend. Ids belong on spans and logs, not on metric dimensions.

## What to watch

- **RED per endpoint** — Rate, Errors, Duration (p50/p95/p99).
- **Saturation** — CPU, memory, GC time, thread-pool queue length, connection pool usage.
- **Dependencies** — latency and error rate per downstream service and per database.
- **Business events** — orders placed, payments declined. These catch failures that are technically healthy.

Alert on symptoms users feel (error rate, p99 latency, queue age), not on causes (CPU at 80%).

## Correlation

`Activity.Current.Id` is the trace id. Put it in every log line (the OpenTelemetry logging provider does this) and in your [error responses](/docs/web/error-handling). Then a support ticket containing one id gives you the whole request across every service.

## Locally

.NET Aspire's dashboard shows traces, metrics, and logs for a whole solution with no external infrastructure — the fastest way to see your instrumentation actually working. See [.NET Aspire](/docs/ops/aspire).

## Further reading

- [OpenTelemetry in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)
