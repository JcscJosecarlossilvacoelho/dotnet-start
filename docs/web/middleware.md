---
title: Middleware and the request pipeline
description: How the pipeline is composed, what the correct order is, and how to write middleware that does not break it.
order: 40
---

Middleware is a chain of `Func<RequestDelegate, RequestDelegate>`. Each component receives the context, may act before calling the next component, and may act again on the way out.

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    await next(context);                             // everything downstream runs here
    var elapsed = Stopwatch.GetElapsedTime(started);
    context.Response.Headers["X-Elapsed-Ms"] = elapsed.TotalMilliseconds.ToString("F1");
});
```

Anything after `await next(context)` runs **after** the response has begun. You cannot change status codes or headers there — check `context.Response.HasStarted` before trying.

## The order that works

```csharp
app.UseExceptionHandler();       // outermost: catches everything below
app.UseHsts();                   // production only
app.UseHttpsRedirection();
app.UseStaticFiles();            // short-circuits before auth for public assets
app.UseRouting();                // selects the endpoint
app.UseCors();                   // after routing, before auth
app.UseRateLimiter();
app.UseAuthentication();         // who are you?
app.UseAuthorization();          // are you allowed? needs the endpoint from UseRouting
app.UseOutputCache();
app.MapControllers();            // executes the endpoint
```

Order is behaviour, not style. Authorization before routing cannot see which policy applies; exception handling registered late cannot catch what ran before it.

## Branching

```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.Map("/admin", admin => admin.UseMiddleware<AdminOnlyMiddleware>());
```

`UseWhen` rejoins the main pipeline; `Map` does not.

## Writing a class-based middleware

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("n");
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            await next(context);
    }
}
```

Rules:

- The class is instantiated **once** for the process. Constructor-injected services are effectively singletons — inject scoped services as `InvokeAsync` parameters instead.
- Always call `next` unless you are deliberately short-circuiting, and write the response when you do.
- Do not swallow exceptions; let `UseExceptionHandler` produce the response.

## Middleware or endpoint filter?

Middleware sees every request, including static files and unmatched routes. An [endpoint filter](/docs/web/minimal-apis) sees only the endpoints it is attached to and can inspect bound arguments. Use middleware for cross-cutting infrastructure and filters for endpoint concerns like validation.

## Further reading

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
