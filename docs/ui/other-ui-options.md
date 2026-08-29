---
title: Razor Pages, MVC views, and MAUI
description: The rest of the UI landscape, and how to choose between them.
order: 50
---

Blazor is not the only option, and for a great many applications it is not the simplest one.

## Razor Pages

Page-focused server rendering: one `.cshtml` file plus a `PageModel` class holding the handlers.

```csharp
public sealed class OrdersModel(IOrderService orders) : PageModel
{
    public IReadOnlyList<Order> Orders { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct) => Orders = await orders.ListAsync(ct);

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken ct)
    {
        await orders.CancelAsync(id, ct);
        return RedirectToPage();
    }
}
```

Choose it for form-driven, mostly-static applications: admin panels, checkout flows, content sites. Server-rendered HTML with a little JavaScript remains the cheapest thing to build, operate, and keep accessible.

## MVC views

Controller-based rendering with `Views/` and `ViewModels`. Well understood, still supported, and the right choice when you already have a large MVC application. For new server-rendered UI, Razor Pages or static Blazor SSR are less ceremony.

## .NET MAUI

One project targeting iOS, Android, macOS, and Windows, with native controls:

```bash
dotnet new maui -n MyApp
dotnet build -t:Run -f net10.0-android
```

Two UI styles: XAML with data binding, or **Blazor Hybrid**, where your Razor components render in a native WebView with full native API access. Blazor Hybrid is compelling when a web version of the same product already exists — the components are shared, the shell is native.

## Choosing

| You need | Use |
| --- | --- |
| Content, SEO, minimal JavaScript | Razor Pages or static Blazor SSR |
| Rich interactivity, .NET all the way | Blazor (see [render modes](/docs/ui/render-modes)) |
| An existing SPA framework (React, Vue) | An ASP.NET Core API + your SPA |
| App stores, native APIs, offline | .NET MAUI or Blazor Hybrid |
| Desktop, Windows only | WPF or WinUI |

## A note on APIs and SPAs

If the frontend is React or Vue, .NET's job is the API. Everything in the [ASP.NET Core](/docs/web/overview) section applies; generate the client from [OpenAPI](/docs/web/openapi) rather than hand-writing fetch calls.

## Further reading

- [Razor Pages](https://learn.microsoft.com/aspnet/core/razor-pages/)
- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/)
