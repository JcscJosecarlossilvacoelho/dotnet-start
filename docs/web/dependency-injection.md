---
title: Dependency injection
description: Lifetimes, registration patterns, and the captive-dependency mistakes that cause production bugs.
order: 50
---

DI is built into the host. Every framework service — configuration, logging, `HttpClient` factories, `DbContext` — is resolved from the same container your own types use.

## Lifetimes

| Lifetime | One instance per | Use for |
| --- | --- | --- |
| `Transient` | Resolution | Cheap, stateless helpers |
| `Scoped` | Request (or explicit scope) | `DbContext`, unit of work, per-request state |
| `Singleton` | Application | Caches, configuration objects, clients that are thread-safe |

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddTransient<IEmailFormatter, EmailFormatter>();
builder.Services.AddHttpClient<PaymentClient>();       // typed client: transient handler, pooled connections
```

## The captive dependency

Injecting a **scoped** service into a **singleton** captures the first instance forever. With `DbContext` this produces intermittent, hard-to-reproduce failures under concurrency. Development builds catch it at startup — keep the check on in every environment:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

When a singleton genuinely needs scoped work, create a scope explicitly:

```csharp
public sealed class Cleanup(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ...
    }
}
```

## Registration patterns

```csharp
services.TryAddScoped<IOrderService, OrderService>();          // do not overwrite an existing registration
services.AddScoped<IValidator, OrderValidator>();
services.AddScoped<IValidator, CustomerValidator>();           // resolve as IEnumerable<IValidator>
services.AddKeyedScoped<IPaymentGateway, StripeGateway>("stripe");
services.AddScoped<IOrderService>(sp => new OrderService(sp.GetRequiredService<AppDbContext>(), "eu-west"));
```

Group registrations per feature in extension methods (`services.AddOrders()`) so `Program.cs` stays readable.

## Options over raw configuration

Bind settings into a typed object and validate at startup, rather than reading `IConfiguration` inside services:

```csharp
builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection("Payments"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Inject `IOptions<T>` for values fixed at startup, `IOptionsMonitor<T>` when they can change at runtime. See [Configuration](/docs/web/configuration).

## Guidance

- Depend on interfaces you own; do not create one interface per class reflexively.
- Constructor injection only — service location (`GetService` inside a method) hides dependencies.
- If a constructor needs more than about five services, the class is doing several jobs.
- Register `TimeProvider` and inject it instead of calling `DateTime.UtcNow`, so time is testable.

## Further reading

- [Dependency injection in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
