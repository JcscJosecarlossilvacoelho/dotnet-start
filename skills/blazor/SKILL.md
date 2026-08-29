---
name: blazor
description: Build, change, or review Blazor interfaces. Use for Razor components, render modes, forms and validation, state management, JavaScript interop, navigation, accessibility, and performance in Blazor Web Apps.
---

# Blazor

Read `Program.cs`, `App.razor`, the layout and route structure, and the nearest existing component before editing. Match the repository's component and styling conventions.

```bash
grep -rE 'AddInteractive|AddRazorComponents|rendermode' Program.cs components app 2>/dev/null | head
```

## Choose the render mode first

It is the decision the rest of the component depends on.

| Content | Mode |
| --- | --- |
| Read-only content, no browser state | Static SSR (no `@rendermode`) |
| Needs events, but server round-trips are fine | `InteractiveServer` |
| Needs offline, low latency, or client-only APIs | `InteractiveWebAssembly` |
| Fast first paint, then client interactivity | `InteractiveAuto` |

Apply interactivity at the **smallest boundary that needs it** — an interactive island inside a static page, not `@rendermode InteractiveServer` at the top of the app. Interactive Server means every keystroke can be a network round-trip; Interactive WebAssembly means every dependency ships to the browser and nothing server-only (a `DbContext`, a connection string, a secret) may be touched.

With prerendering on, a component's lifecycle runs **twice**. Write `OnInitializedAsync` so running it twice is harmless, and gate JS interop on `OnAfterRenderAsync(firstRender)` — `IJSRuntime` is unavailable during prerender.

## Component boundaries

```csharp
[Parameter, EditorRequired] public required Order Order { get; set; }
[Parameter] public EventCallback<OrderId> OnCancelled { get; set; }
```

- Inputs are `[Parameter]`; outputs are `EventCallback`/`EventCallback<T>` (they marshal to the right thread and re-render the parent — a raw `Action` does not).
- Mark required parameters `[EditorRequired]`. Never mutate your own parameters; treat them as read-only inputs.
- Give items in a loop a stable `@key` so the diff does not reuse the wrong DOM node.
- Reach for a scoped state container (or a cascading value) only when state genuinely spans routes or distant components. Parameters first.
- Anything you subscribe to, unsubscribe in `IDisposable`/`IAsyncDisposable` — a component that stays subscribed after disposal leaks the whole render tree.
- Call `StateHasChanged` only from events outside the renderer (a timer, a service callback), and via `InvokeAsync(StateHasChanged)`.

## Forms

```razor
<EditForm Model="_model" OnValidSubmit="SubmitAsync" FormName="checkout">
    <DataAnnotationsValidator />
    <label for="sku">SKU</label>
    <InputText id="sku" @bind-Value="_model.Sku" aria-describedby="sku-error" />
    <ValidationMessage For="() => _model.Sku" id="sku-error" />
    <button type="submit" disabled="@_busy">@(_busy ? "Saving…" : "Save")</button>
</EditForm>
```

- One explicit model type per form, validated server-side as well — client validation is a convenience, never a control.
- Keep each message beside its field, preserve submitted values after a failure, and disable the submit button while in flight so a double click cannot double post.
- Give static-SSR forms a `FormName` and keep the antiforgery middleware on.

## Accessibility and UX

- Semantic HTML first: a `<button>` for actions, an `<a>` for navigation. A clickable `<div>` is not keyboard reachable.
- Every control needs an accessible name (`<label for>` or `aria-label`), a visible focus ring, and a meaningful disabled/loading state.
- Reserve layout space for async content so it does not jump; show a skeleton, not a spinner that resizes the page.
- Announce async results to assistive tech with `aria-live` on the region that changes.
- Build mobile-first, honour `prefers-reduced-motion`, and keep normal-text contrast at least 4.5:1.
- Surface recoverable errors in the component that started the action; wrap risky subtrees in `<ErrorBoundary>` so one failure does not blank the page.

## JavaScript interop

Use interop for browser-only capabilities (clipboard, canvas, an existing JS widget), not for behavior Razor and CSS already express. Isolate it in a module and dispose it:

```csharp
_module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Chart.razor.js");
```

Keep interop out of `OnInitializedAsync`, and treat a disconnected circuit or a cancelled call as a normal state, not an exception to log as an error.

## Performance

- Interactive Server sends every event over the circuit — avoid `@oninput` on large forms, `@bind:event="oninput"` without a debounce, and per-keystroke server queries.
- Use `Virtualize` for long lists instead of rendering thousands of rows.
- Prefer `ShouldRender` or splitting a component over re-rendering an expensive subtree; compute derived values in `OnParametersSet`, not in the markup.
- Watch WebAssembly payload size; lazy-load assemblies for routes that are rarely visited.

## Complete the change

```bash
dotnet build -warnaserror
dotnet test
dotnet run    # then open the route
```

Verify the rendered route in a browser at desktop width and at 375 px, exercise the primary interaction with the keyboard alone, and check the browser console and server log for circuit errors.
