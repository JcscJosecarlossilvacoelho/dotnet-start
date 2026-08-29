---
title: CORS and web security
description: The headers, policies, and defaults that decide whether your API is safe in a browser.
order: 160
---

## CORS

The browser blocks cross-origin reads unless the server opts in. CORS is a **browser** mechanism: it does not protect you from anything that is not a browser.

```csharp
builder.Services.AddCors(options =>
    options.AddPolicy("spa", policy => policy
        .WithOrigins("https://app.example.com")   // never AllowAnyOrigin with credentials
        .AllowCredentials()
        .WithHeaders("Content-Type", "Authorization")
        .WithMethods("GET", "POST", "PUT", "DELETE")));

app.UseCors("spa");     // after UseRouting, before UseAuthorization
```

`AllowAnyOrigin()` combined with `AllowCredentials()` is rejected by the framework, because it would let any site read authenticated responses. If you find yourself reaching for both, the design is wrong.

## Security headers

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Frame-Options"] = "DENY";
    headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";
    await next(context);
});

app.UseHsts();               // production only; browsers cache it aggressively
app.UseHttpsRedirection();
```

A strict CSP is the single most effective defence against XSS. Start in report-only mode, watch the reports, then enforce.

## Antiforgery

Cookie-authenticated form posts need antiforgery tokens; token-authenticated APIs do not (the token is not sent automatically). Blazor and Razor Pages wire this up when you call `app.UseAntiforgery()`.

## Input and output

- **SQL injection** — use parameters. EF Core and `SqlParameter` do this; string concatenation into SQL never does. See [Raw SQL](/docs/data/dapper-and-ado).
- **XSS** — Razor and Blazor encode by default. `MarkupString` and `Html.Raw` opt out; sanitise anything user-supplied before using them.
- **Path traversal** — never build a file path from user input; map an id to a path server-side.
- **Deserialization** — never enable polymorphic type handling from untrusted input.
- **SSRF** — validate outbound URLs against an allowlist when the target comes from a request.

## Secrets and dependencies

- Keep secrets out of the repository — see [Configuration](/docs/web/configuration).
- Run `dotnet list package --vulnerable --include-transitive` in CI and fail on findings.
- Enable HTTPS everywhere, including between internal services where it is cheap to do so.

## Uploads

Limit size (`RequestSizeLimit`), validate the content type *and* the magic bytes, store outside the web root, and never trust the client's file name.

## Further reading

- [ASP.NET Core security](https://learn.microsoft.com/aspnet/core/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
