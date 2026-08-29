---
title: JavaScript interop
description: Calling JavaScript from C# and back, with modules, disposal, and prerendering handled correctly.
order: 40
---

Blazor does not replace the JavaScript ecosystem. Charts, maps, editors, and browser APIs are one call away.

## Calling JavaScript from C#

Ship your JavaScript as an ES module and import it lazily:

```js
// wwwroot/js/clipboard.js
export function copy(text) {
  return navigator.clipboard.writeText(text).then(() => true, () => false);
}
```

```razor
@implements IAsyncDisposable
@inject IJSRuntime JS

<button @onclick="Copy">Copy</button>

@code {
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/clipboard.js");
    }

    private async Task Copy()
    {
        if (_module is not null)
            await _module.InvokeAsync<bool>("copy", "text to copy");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
```

Modules keep the global namespace clean and load only when the component that needs them renders.

## Calling C# from JavaScript

```csharp
private DotNetObjectReference<MyComponent>? _self;

protected override void OnInitialized() => _self = DotNetObjectReference.Create(this);

[JSInvokable]
public Task OnResize(int width) { _width = width; return InvokeAsync(StateHasChanged); }

public void Dispose() => _self?.Dispose();
```

```js
export function observe(element, dotnetRef) {
  const observer = new ResizeObserver(entries =>
    dotnetRef.invokeMethodAsync('OnResize', entries[0].contentRect.width));
  observer.observe(element);
  return { dispose: () => observer.disconnect() };
}
```

Every `DotNetObjectReference` you create must be disposed, and every JS-side resource you create must be torn down — otherwise you have leaked a component and its whole object graph.

## Element references

```razor
<div @ref="_container"></div>

@code {
    private ElementReference _container;
}
```

`ElementReference` is only valid after the element has rendered. Pass it to JavaScript; never try to read DOM properties from C#.

## Prerendering

`IJSRuntime` is unavailable while prerendering — there is no browser yet. Interop belongs in `OnAfterRenderAsync`. Attempting it earlier throws `InvalidOperationException`, which is one of the most common Blazor errors.

## Cost model

- **WebAssembly** — calls are in-process and fast, but marshalling large objects still costs.
- **Interactive Server** — every call is a round trip over the circuit. Batch chatty interop into one call, and never put interop in a loop over a list.

## Further reading

- [Call JavaScript from .NET](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet)
