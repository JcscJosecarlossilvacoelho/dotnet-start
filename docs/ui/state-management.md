---
title: State management
description: Where state lives in a Blazor application, and how to share it without creating leaks.
order: 30
---

Blazor has no prescribed state library. Choose per scope, and prefer the smallest one that works.

## Scopes, smallest first

| Scope | Mechanism |
| --- | --- |
| One component | A private field |
| Parent and children | `[Parameter]` down, `EventCallback` up |
| A subtree | `<CascadingValue>` |
| One user's session | A scoped service (Server) or a singleton (WebAssembly) |
| Across reloads | `localStorage`/`sessionStorage` via interop, or the URL |
| Across users | The database |

Most "we need a state library" moments are really "these two components should be one component".

## A shared state service

```csharp
public sealed class CartState
{
    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;
    public event Action? Changed;

    public void Add(CartLine line)
    {
        _lines.Add(line);
        Changed?.Invoke();
    }
}

builder.Services.AddScoped<CartState>();   // per circuit on Server, per app on WebAssembly
```

```razor
@implements IDisposable
@inject CartState Cart

<p>@Cart.Lines.Count items</p>

@code {
    protected override void OnInitialized() => Cart.Changed += OnChanged;
    private void OnChanged() => InvokeAsync(StateHasChanged);
    public void Dispose() => Cart.Changed -= OnChanged;
}
```

Two rules make this safe: **always unsubscribe** in `Dispose` (a forgotten handler keeps a whole component tree alive), and **always marshal to the renderer** with `InvokeAsync(StateHasChanged)` when the event may come from another thread.

## The URL is state

Filters, selected tabs, and pagination belong in the query string. It costs nothing, survives refresh, and makes every view linkable:

```razor
@page "/orders"
@code {
    [SupplyParameterFromQuery] public string? Status { get; set; }
    [SupplyParameterFromQuery] public int Page { get; set; } = 1;
}
```

## Surviving prerender

State fetched during prerendering is lost when the component activates, causing a second fetch and a visible flicker. Persist it:

```csharp
private PersistingComponentStateSubscription _subscription;

protected override async Task OnInitializedAsync()
{
    _subscription = State.RegisterOnPersisting(() =>
    {
        State.PersistAsJson(nameof(_orders), _orders);
        return Task.CompletedTask;
    });

    if (!State.TryTakeFromJson<List<Order>>(nameof(_orders), out _orders))
        _orders = await Api.GetOrdersAsync();
}
```

## What not to keep in memory

On Interactive Server, per-user state lives on the server. Ten thousand users each holding a large list is ten thousand copies. Keep circuits light: hold ids and page-sized data, fetch the rest on demand, and never treat a circuit as a session store for large objects.

## Further reading

- [Blazor state management](https://learn.microsoft.com/aspnet/core/blazor/state-management)
