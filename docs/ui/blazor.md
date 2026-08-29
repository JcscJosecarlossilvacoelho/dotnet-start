---
title: Blazor
description: Components, rendering, and the model behind a Blazor Web App.
order: 10
---

Blazor builds UI from **components**: `.razor` files that mix markup with C#. The same component model runs on the server, in WebAssembly, and inside native apps.

## A component

```razor
@* Counter.razor *@
<button class="button" @onclick="Increment">Clicked @_count times</button>

@code {
    [Parameter] public int Step { get; set; } = 1;
    [Parameter] public EventCallback<int> OnChanged { get; set; }

    private int _count;

    private async Task Increment()
    {
        _count += Step;
        await OnChanged.InvokeAsync(_count);
    }
}
```

- `[Parameter]` declares input. Parameters flow **down**; events flow **up** via `EventCallback`.
- The component re-renders after an event handler completes. Call `StateHasChanged()` only when state changed outside the normal flow (a timer, a message from a background service).
- `@key` on a list item keeps identity stable across re-renders and prevents the diff from reusing the wrong element.

## Lifecycle

| Method | When |
| --- | --- |
| `OnInitialized{Async}` | Once, when the component is created |
| `OnParametersSet{Async}` | Every time parameters change |
| `ShouldRender` | Before each render — return false to skip |
| `OnAfterRender{Async}` | After the DOM is updated; `firstRender` distinguishes the first pass |
| `IAsyncDisposable` | Clean up subscriptions, timers, JS objects |

Do JavaScript interop in `OnAfterRenderAsync`, never in `OnInitializedAsync` — during prerendering there is no DOM yet.

## Composition

```razor
<Card Title="Orders">
    <Body>
        @foreach (var order in Orders)
        {
            <OrderRow @key="order.Id" Order="order" OnSelected="Select" />
        }
    </Body>
</Card>
```

`RenderFragment` parameters (like `Body` above) are how components accept markup. Cascading values (`<CascadingValue>`) pass ambient state such as the current theme or user down a subtree without threading it through every parameter.

## Routing and layout

```razor
@page "/orders/{Id:guid}"
@layout MainLayout

@code { [Parameter] public Guid Id { get; set; } }
```

Routes are declared on the component. `NavigationManager` navigates and reads the current URL; `NavLink` renders links with an active class.

## Forms

```razor
<EditForm Model="_model" OnValidSubmit="Save" FormName="createOrder">
    <DataAnnotationsValidator />
    <InputText @bind-Value="_model.Reference" class="input" />
    <ValidationMessage For="() => _model.Reference" />
    <button type="submit" class="button button-primary">Save</button>
</EditForm>
```

`EditForm` builds an `EditContext` that tracks modification and validation state. `FormName` is required for static server-side forms so the post can be routed back to the right form.

## Where to go next

- [Render modes](/docs/ui/render-modes) — the decision that shapes everything else.
- [JavaScript interop](/docs/ui/javascript-interop) — using the ecosystem you already have.
- [State management](/docs/ui/state-management) — keeping state where it belongs.

## Further reading

- [Blazor documentation](https://learn.microsoft.com/aspnet/core/blazor/)
