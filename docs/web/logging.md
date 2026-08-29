---
title: Logging
description: Structured logging with ILogger, scopes, levels, and how to make logs searchable rather than decorative.
order: 70
---

`ILogger<T>` is registered for you. What matters is *how* you call it: log **structured events**, not sentences.

## Structured, not interpolated

```csharp
logger.LogInformation("Order {OrderId} placed by {CustomerId} for {Total}", order.Id, order.CustomerId, order.Total);
```

The message template is the event identity; the values become searchable fields. Never use string interpolation (`$"..."`) — it destroys the structure and formats even when the level is disabled.

## Levels, and what they mean here

| Level | Use for |
| --- | --- |
| `Trace` | Developer-only detail; never on in production |
| `Debug` | Diagnostic detail while investigating |
| `Information` | Business events worth counting: order placed, payment captured |
| `Warning` | Something recoverable and unexpected: a retry, a degraded dependency |
| `Error` | An operation failed and a user is affected |
| `Critical` | The process cannot continue |

Configure per category so a noisy library does not drown the signal:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

## Scopes add context to everything inside them

```csharp
using (logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = order.Id }))
{
    await ChargeAsync(order, ct);   // every log inside carries OrderId
}
```

## Source-generated log methods

The allocation-free, analyser-approved form:

```csharp
internal static partial class Log
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Payment for order {OrderId} was declined: {Reason}")]
    public static partial void PaymentDeclined(ILogger logger, Guid orderId, string reason);
}

Log.PaymentDeclined(logger, order.Id, "insufficient_funds");
```

No boxing, no formatting unless the level is enabled, and a stable event id you can alert on.

## What not to log

- Passwords, tokens, API keys, full card numbers, national identifiers.
- Whole request or response bodies by default — they contain the above.
- An exception you are rethrowing (the outer handler will log it once).

## Where logs go

The console provider writes to stdout, which is what containers and orchestrators collect. In production, emit JSON so the collector can parse fields:

```csharp
builder.Logging.AddJsonConsole();
```

For distributed systems, export logs alongside traces and metrics with OpenTelemetry — see [Observability](/docs/ops/observability). Correlating a log line with a trace id is what turns logs from a diary into a debugger.

## Further reading

- [Logging in .NET](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Compile-time logging source generation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator)
