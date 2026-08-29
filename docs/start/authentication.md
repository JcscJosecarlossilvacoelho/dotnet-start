---
title: Authentication
description: Cookie or bearer auth with the framework primitives — not a custom filter.
order: 30
---

# Authentication

Use the authentication and authorization stack that ships with ASP.NET Core. Do not invent a middleware that inspects headers by hand.

## Choose a scheme

| You are building | Use |
| --- | --- |
| A browser app (Blazor, MVC) | Cookie authentication, often with ASP.NET Core Identity |
| An API consumed by a SPA or mobile client | Bearer tokens (JWT) issued by your identity provider |
| Both | One identity provider, two schemes, policies that do not care which one ran |

For a first API, **JWT bearer** against a known issuer is the usual cut. For a first Blazor app, **cookies + Identity** is less moving gear.

## Register it in Program.cs

```csharp
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

Order matters: authentication before authorization, both before the endpoints that depend on them.

## Protect the endpoint, not the controller guts

```csharp
app.MapGet("/me", (ClaimsPrincipal user) =>
    Results.Ok(new { name = user.Identity?.Name }))
    .RequireAuthorization();
```

The requirement lives next to the route. Anyone reading `Program.cs` can see what is public.

## Build it with an agent

> Add JWT bearer authentication and authorization to this ASP.NET Core API. Protect GET /me so it returns the current user's name. Keep Program.cs as the composition root, do not write custom header-parsing middleware, and add an integration test for 401 and 200.

## Next

If the API is for humans in a browser, put a Blazor UI in front of it.
