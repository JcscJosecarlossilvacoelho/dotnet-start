---
title: Error handling
description: One place to turn exceptions into HTTP responses, without leaking internals.
order: 90
---

Handle errors once, at the boundary. Scattering try/catch through handlers produces inconsistent responses and hides bugs.

## The baseline

```csharp
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
```

Every unhandled exception becomes a `ProblemDetails` response; every bare status code gets a body instead of an empty response.

## Mapping your exceptions

```csharp
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            OrderNotFoundException     => (StatusCodes.Status404NotFound, "Order not found"),
            ConcurrencyException       => (StatusCodes.Status409Conflict, "The order changed while you were editing it"),
            PaymentDeclinedException   => (StatusCodes.Status402PaymentRequired, "Payment declined"),
            _                          => (0, "")
        };

        if (status == 0) return false;      // not ours: let the next handler deal with it

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message }
        });
    }
}

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
```

Handlers run in registration order; returning `false` passes the exception along.

## Enriching every problem response

```csharp
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    });
```

A `traceId` in the response body is the single most useful field for support: it links a user's screenshot to a [trace](/docs/ops/observability).

## What never goes in a response

Stack traces, SQL, connection strings, internal host names, and the existence of records the caller may not see. In development the developer exception page is fine; in production the response says what the client can act on and the log holds the rest.

## Status code guidance

| Situation | Status |
| --- | --- |
| Invalid input | 400 (+ `errors`) |
| Not authenticated | 401 |
| Authenticated but not allowed | 403 |
| Resource does not exist, or must appear not to | 404 |
| Conflicting state, optimistic concurrency | 409 |
| Rate limited | 429 (+ `Retry-After`) |
| Bug | 500 |
| Dependency down, deliberate shed | 503 (+ `Retry-After`) |

## Client-side cancellation

When a caller disconnects, the request's `CancellationToken` is cancelled and an `OperationCanceledException` propagates. That is not an error: filter it out of your error logs and metrics, or your dashboards will lie.

## Further reading

- [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
