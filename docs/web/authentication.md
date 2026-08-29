---
title: Authentication
description: How identity arrives with a request — JWT bearer tokens, cookies, OpenID Connect, and API keys.
order: 120
---

Authentication answers *who is calling*. It populates `HttpContext.User` with a `ClaimsPrincipal` and stops there; deciding what they may do is [authorization](/docs/web/authorization).

## Choosing a scheme

| Scenario | Scheme |
| --- | --- |
| API called by other services or SPAs | JWT bearer, issued by an identity provider |
| Server-rendered app with a browser session | Cookies, usually via OpenID Connect |
| Machine-to-machine inside a trust boundary | mTLS or a signed token |
| Public webhook receiver | Signature verification (not a scheme; verify in middleware) |

Do not write your own token format or password hashing. Use an identity provider (Entra ID, Auth0, Keycloak, Okta) or ASP.NET Core Identity if you must own the store.

## JWT bearer

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];   // discovers keys from /.well-known
        options.Audience = "orders-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)     // default is 5 minutes — tighten it
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

Validate issuer, audience, lifetime, and signature. Disabling any of them turns the token into a suggestion.

## Cookies and OpenID Connect

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Auth:Authority"];
    options.ClientId = builder.Configuration["Auth:ClientId"];
    options.ClientSecret = builder.Configuration["Auth:ClientSecret"];
    options.ResponseType = "code";                 // authorization code + PKCE
    options.SaveTokens = true;
    options.Scope.Add("offline_access");
});
```

The authorization code flow with PKCE is the only browser flow to use. Implicit flow is deprecated.

## Multiple schemes

```csharp
app.MapGet("/internal", Handler)
   .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "ApiKey" });
```

A single application can accept cookies for its UI and bearer tokens for its API.

## Reading identity

```csharp
app.MapGet("/me", (ClaimsPrincipal user) => new
{
    Subject = user.FindFirstValue(ClaimTypes.NameIdentifier),
    Email   = user.FindFirstValue(ClaimTypes.Email),
    Scopes  = user.FindFirstValue("scope")?.Split(' ') ?? []
}).RequireAuthorization();
```

## Data protection

Cookies, antiforgery tokens, and anything else `IDataProtector` encrypts are tied to a key ring. In a multi-instance deployment, persist that ring to shared storage or every restart or scale-out event logs your users out:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("MyApp");
```

## Further reading

- [Authentication overview](https://learn.microsoft.com/aspnet/core/security/authentication/)
