---
name: aspnet-core
description: Build, change, or review ASP.NET Core APIs and services. Use for Minimal APIs, controllers, middleware, dependency injection, validation, authentication and authorization, OpenAPI, error handling, and production-ready HTTP behavior.
---

# ASP.NET Core

Read the target framework, the project file, `Program.cs`, and the nearest existing endpoint and its test before writing anything. Preserve the project's established API style unless the change itself justifies a deliberate migration.

```bash
grep -E 'TargetFramework|PackageReference' *.csproj **/*.csproj 2>/dev/null
sed -n '1,80p' Program.cs
```

## Design the contract before the code

Write down route, method, request shape, response shape, status codes, and who is allowed to call it. Everything below follows from that.

| Status | Use for |
| --- | --- |
| 200 / 201 / 204 | read / created (with `Location`) / successful command with no body |
| 400 | malformed or invalid input |
| 401 / 403 | not authenticated / authenticated but not permitted |
| 404 | resource absent, or hidden from this caller |
| 409 | conflicts with current state (duplicate, concurrency, out of stock) |
| 422 | syntactically valid but semantically rejected, when you distinguish it from 400 |

Return RFC 9457 problem details for every failure, from one place:

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();   // + an IExceptionHandler that maps domain failures to status codes
```

## Minimal APIs or controllers

Prefer **Minimal APIs** for new focused endpoints; prefer **controllers** when the app already leans on MVC conventions, filters, or model binders. Do not mix both styles for the same resource.

```csharp
var orders = app.MapGroup("/orders")
    .RequireAuthorization("orders:read")
    .WithTags("Orders");

orders.MapGet("/{id:int}", async (int id, IOrderStore store, CancellationToken ct) =>
        await store.FindAsync(id, ct) is { } order
            ? Results.Ok(OrderResponse.From(order))
            : Results.NotFound())
    .WithName("GetOrder")
    .Produces<OrderResponse>()
    .Produces(StatusCodes.Status404NotFound);
```

- Group related routes with `MapGroup` and hang auth, filters, and tags off the group rather than repeating them.
- Keep handlers thin: bind, delegate, map the result. Business rules belong in a service you can test without HTTP.
- Bind a `CancellationToken` parameter and flow it into every I/O call.
- Use route constraints (`{id:int}`, `{slug:regex(...)}`) so bad input fails at routing instead of inside your handler.

## Never expose entities on the wire

Define request and response records at the boundary. Sharing a persistence entity with the API is how you get over-posting, accidental data leaks, and a schema you can no longer change.

```csharp
public sealed record CreateOrderRequest(string Sku, int Quantity);
public sealed record OrderResponse(int Id, string Sku, int Quantity, decimal Total);
```

## Validate at the edge

Validate in the endpoint or a filter, before any domain call, and return one problem-details payload listing every field error — not the first one.

```csharp
app.MapPost("/orders", (CreateOrderRequest r, IOrderService svc) => ...)
   .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

Use the framework's built-in Minimal API validation where the target framework supports it, otherwise FluentValidation or `IValidatableObject` — pick the one already in the repo.

## Dependency injection

- Register services with the narrowest correct lifetime. **Never resolve a scoped service from a singleton** — that is the most common ASP.NET Core bug, and it silently reuses a `DbContext` across requests.
- Use `IServiceScopeFactory` when a singleton or background service needs scoped work.
- Bind configuration with the options pattern and validate it at startup:
  ```csharp
  builder.Services.AddOptions<PaymentOptions>()
      .BindConfiguration("Payments").ValidateDataAnnotations().ValidateOnStart();
  ```
- Get secrets from user-secrets, environment variables, or a vault. Never from `appsettings.json`.
- Prefer typed `HttpClient`s via `IHttpClientFactory` over `new HttpClient()`.

## Middleware order matters

```csharp
app.UseExceptionHandler();
app.UseHsts();               // non-development
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Authentication always precedes authorization; exception handling wraps everything. Write custom middleware only for genuinely cross-cutting concerns — an endpoint filter is the better tool for one route or group.

## Security defaults

- Authorize by **policy**, not by scattered role strings; deny by default and opt endpoints out explicitly with `AllowAnonymous`.
- Validate token issuer, audience, and lifetime. Never accept an unsigned or unvalidated JWT.
- Configure a named CORS policy with explicit origins. `AllowAnyOrigin` with credentials is invalid and unsafe.
- Add rate limiting on public and authentication endpoints.
- Enforce a request body size limit; do not echo raw exception detail to clients in production.

## Complete the change

```bash
dotnet build -warnaserror
dotnet test
```

Verify the HTTP contract with an integration test through `WebApplicationFactory`, asserting status code and payload shape — not internal calls. Check the OpenAPI document still describes the endpoint correctly. Report any contract decision that remained an assumption.
