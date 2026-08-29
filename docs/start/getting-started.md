---
title: First endpoint
description: Install the SDK, scaffold an API, and return JSON from one route.
order: 10
---

# First endpoint

The shortest path from nothing to a running web API. One SDK, one template, one route.

## Before you start

Install the current SDK from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download), then confirm it:

```bash
dotnet --version
```

You want a **10.x** version. Anything else: install the SDK before continuing.

## Create the project

The `webapi` template gives you OpenAPI and the essentials already wired.

```bash
dotnet new webapi -n MyApp
cd MyApp
dotnet run
```

The terminal prints the listening address. Open it.

## Define the endpoint

Open `Program.cs` and make the contract explicit. The return value is inferred and serialized as JSON.

```csharp
app.MapGet("/hello/{name}", (string name) =>
{
    return Results.Ok(new { message = $"Hello, {name}!" });
});
```

Hit `GET /hello/world` and you should see `{ "message": "Hello, world!" }`.

## Build it with an agent

Paste this into Claude or Codex:

> Create an ASP.NET Core API on .NET 10 named `MyApp`. Add a `GET /hello/{name}` endpoint, keep the project simple, and explain each change. Then build the project and add an HTTP test that validates the response.

## Next

Wire a database with Entity Framework Core — without hiding what happens underneath.
