---
title: Routing
description: How URLs are matched, route constraints, precedence rules, and generating links.
order: 30
---

Routing runs in two stages: `UseRouting` selects an endpoint by matching the URL against the route table, and `UseEndpoints` (implicit at the end of the pipeline) executes it. Middleware placed between the two can inspect the chosen endpoint and its metadata — this is how authorization knows which policy applies.

## Route templates

```csharp
app.MapGet("/orders/{id:guid}", ...);              // typed segment
app.MapGet("/orders/{id:guid}/lines/{index:int}", ...);
app.MapGet("/files/{**path}", ...);                // catch-all, slashes preserved
app.MapGet("/reports/{year:int:min(2000)}", ...);  // multiple constraints
app.MapGet("/search/{term?}", ...);                // optional
app.MapGet("/page/{number:int=1}", ...);           // default value
```

## Constraints

| Constraint | Matches |
| --- | --- |
| `int`, `long`, `decimal`, `double`, `bool` | Parseable values |
| `guid` | A GUID |
| `datetime` | A parseable date |
| `alpha` | Letters only |
| `minlength(n)`, `maxlength(n)`, `length(n,m)` | String length |
| `min(n)`, `max(n)`, `range(n,m)` | Numeric range |
| `regex(...)` | A pattern — expensive; prefer a specific constraint |

Constraints exist for **disambiguation**, not validation. A request that fails a constraint gets a 404, not a 400 with a helpful message. Validate values in the handler; see [Validation](/docs/web/validation).

## Precedence

When several routes could match, the most specific wins:

1. More literal segments beat fewer.
2. A literal segment beats a parameter.
3. A constrained parameter beats an unconstrained one.
4. A catch-all loses to everything else.

Two routes of equal precedence throw `AmbiguousMatchException` at request time — a failure worth catching in an integration test.

## Generating links

Never concatenate URLs by hand. Name the endpoint and generate:

```csharp
orders.MapGet("/{id:guid}", GetOrder).WithName("GetOrder");

var url = linkGenerator.GetPathByName("GetOrder", new { id });
return TypedResults.Created(url, order);
```

`LinkGenerator` works anywhere, including background services; `IUrlHelper` is request-scoped.

## Host, method, and other matchers

```csharp
app.MapGet("/health", () => Results.Ok()).RequireHost("internal.example.com");
app.MapMethods("/webhook", ["POST", "PUT"], Handle);
app.MapFallbackToFile("index.html");   // SPA hosting
```

## Case, culture, and trailing slashes

Route matching is case-insensitive by default. Generated links are lowercase if you configure `RouteOptions.LowercaseUrls = true`. Trailing slashes are not equivalent unless you opt in with `AppendTrailingSlash` — pick one form and redirect the other for consistent caching and analytics.

## Further reading

- [Routing in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/routing)
