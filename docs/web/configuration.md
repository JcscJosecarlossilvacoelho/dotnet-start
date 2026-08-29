---
title: Configuration and secrets
description: How settings are layered, how to bind and validate them, and where secrets should actually live.
order: 60
---

Configuration is a set of key/value providers read in order; later providers override earlier ones.

## The default order

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (Development only)
4. Environment variables
5. Command-line arguments

The environment comes from `ASPNETCORE_ENVIRONMENT` (or `DOTNET_ENVIRONMENT`), and its conventional values are `Development`, `Staging`, and `Production`.

## Keys and nesting

Nested JSON flattens with `:` separators. Environment variables use `__` (double underscore), which works on every platform:

```json
{ "Payments": { "ApiKey": "...", "TimeoutSeconds": 10 } }
```

```bash
export Payments__TimeoutSeconds=30
```

## Bind to a typed object

```csharp
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    [Required] public required string ApiKey { get; init; }
    [Range(1, 120)] public int TimeoutSeconds { get; init; } = 10;
}

builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`ValidateOnStart` turns a missing setting into a startup failure instead of a 500 at 3 a.m.

| Interface | Reads | Lifetime |
| --- | --- | --- |
| `IOptions<T>` | Once, at first resolution | Singleton |
| `IOptionsSnapshot<T>` | Per request | Scoped |
| `IOptionsMonitor<T>` | Live, with change notifications | Singleton |

## Secrets

**Never** commit a secret to `appsettings.json`. In development:

```bash
dotnet user-secrets init
dotnet user-secrets set "Payments:ApiKey" "sk_test_..."
```

User secrets live outside the repository, in your profile folder.

In production, use the platform's secret store and let it surface as configuration:

- Environment variables injected by the orchestrator (the simplest correct answer).
- Azure Key Vault via `AddAzureKeyVault`, AWS Secrets Manager, HashiCorp Vault.
- Kubernetes secrets mounted as files, read with `AddKeyPerFile`.

Rotate by restarting or by using `IOptionsMonitor` with a reloading provider. Never log a configuration object wholesale — it will contain the secret you were careful about.

## Connection strings

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
```

Fail loudly at startup. A null connection string that surfaces on the first request is a much worse failure.

## Further reading

- [Configuration in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Options pattern](https://learn.microsoft.com/dotnet/core/extensions/options)
