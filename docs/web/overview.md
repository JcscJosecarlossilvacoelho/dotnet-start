---
title: How ASP.NET Core works
description: The host, the server, the pipeline, and where your code sits inside them.
order: 10
---

Every ASP.NET Core application is the same four layers, whatever template you started from.

## The layers

1. **Host** — owns configuration, logging, dependency injection, and the lifetime of the process. Built by `WebApplication.CreateBuilder(args)`.
2. **Server** — Kestrel by default. Accepts TCP connections, parses HTTP/1.1, HTTP/2, and HTTP/3, and produces an `HttpContext` per request.
3. **Middleware pipeline** — an ordered chain of delegates that each see the request on the way in and the response on the way out.
4. **Endpoint** — the terminal handler: a minimal API delegate, a controller action, a Razor component, a gRPC method.

```csharp
var builder = WebApplication.CreateBuilder(args);   // 1. host + services

builder.Services.AddOpenApi();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.UseExceptionHandler();      // 3. pipeline, in order
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/orders/{id:guid}", (Guid id, IOrderService orders) => orders.GetAsync(id));  // 4. endpoint

app.Run();                      // 2. start Kestrel and block
```

Everything above the `Build()` call configures services; everything below configures the pipeline. That split is the single most useful thing to hold in your head when reading someone else's `Program.cs`.

## The request lifetime

A request creates a **DI scope**. Scoped services — `DbContext`, unit-of-work types, per-request caches — live exactly as long as that scope and are disposed when the response completes. Work that escapes the request (an unawaited task) escapes the scope and will fail on disposed dependencies.

## Hosting models

| Model | When |
| --- | --- |
| Kestrel directly | The default; fine behind a load balancer or ingress |
| Kestrel behind a reverse proxy (nginx, YARP, IIS) | TLS termination, shared ports, legacy infrastructure |
| Container | The normal production shape — see [containers](/docs/ops/containers) |

Behind a proxy, add `UseForwardedHeaders` so `Request.Scheme` and the client IP reflect the original request rather than the proxy hop.

## The pieces this section documents

- [Minimal APIs](/docs/web/minimal-apis) and [controllers](/docs/web/controllers) — the two endpoint models.
- [Routing](/docs/web/routing), [middleware](/docs/web/middleware), [model binding and validation](/docs/web/validation).
- [Dependency injection](/docs/web/dependency-injection), [configuration](/docs/web/configuration), [logging](/docs/web/logging).
- [Authentication](/docs/web/authentication) and [authorization](/docs/web/authorization).
- [OpenAPI](/docs/web/openapi), [HTTP clients](/docs/web/http-client), [error handling](/docs/web/error-handling), [caching](/docs/web/caching), [rate limiting](/docs/web/rate-limiting).

## Further reading

- [ASP.NET Core fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/)
