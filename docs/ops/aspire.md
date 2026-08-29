---
title: .NET Aspire
description: Composing a multi-service application locally, with service discovery, telemetry, and deployment manifests.
order: 60
---

.NET Aspire is an opinionated stack for applications made of several processes and backing services. It gives you one command to run everything, a dashboard for the whole system, and defaults for telemetry, health, and resilience.

## The app host

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("db").WithPgAdmin().AddDatabase("orders");
var cache    = builder.AddRedis("cache");

var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(postgres)
    .WithReference(cache);

builder.AddProject<Projects.MyApp_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

```bash
dotnet run --project MyApp.AppHost
```

Containers start, connection strings are injected, and the dashboard opens showing every service's logs, traces, metrics, and health.

## Service defaults

The generated `ServiceDefaults` project is the interesting part — it is ordinary ASP.NET Core code you can read and change:

```csharp
builder.AddServiceDefaults();   // OpenTelemetry, health checks, service discovery, HTTP resilience
```

Service discovery means the API calls its dependency by name:

```csharp
builder.Services.AddHttpClient<OrdersClient>(client => client.BaseAddress = new Uri("https+http://api"));
```

No connection strings in `appsettings.json`, no port juggling between machines.

## Components

`builder.AddNpgsqlDbContext<AppDbContext>("orders")` and its siblings register a client that is already instrumented, health-checked, and resilient. That is most of what Aspire buys you: consistent, correct defaults for the boring parts.

## Deployment

Aspire produces a manifest describing the resources and their relationships; `azd` deploys it to Azure Container Apps, and `aspir8` generates Kubernetes manifests. Aspire does not replace your infrastructure-as-code — it describes the topology and hands it over.

## When to use it

Use it when you run several processes locally (an API, a worker, a UI, a database, a cache) and starting them is a chore. Skip it for a single service with one database, where Docker Compose and a connection string are less machinery.

Adopting it is not all-or-nothing: `AddServiceDefaults` is useful on its own, even without an app host.

## Further reading

- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
