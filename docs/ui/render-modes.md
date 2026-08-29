---
title: Render modes
description: Static, Server, WebAssembly, and Auto — what each one costs and how to choose per component.
order: 20
---

A Blazor Web App can render each component differently. This is the most consequential decision in a Blazor application, and it is made per component rather than per application.

## The modes

| Mode | Runs | Interactive | First paint | Trade-off |
| --- | --- | --- | --- | --- |
| Static SSR | Server, once per request | No | Fastest | No client state; forms post like HTML |
| Interactive Server | Server, over a SignalR circuit | Yes | Fast | Latency per interaction; server memory per user |
| Interactive WebAssembly | Browser | Yes | Slower (runtime download) | Works offline; API calls need real endpoints |
| Interactive Auto | Server first, WebAssembly once cached | Yes | Fast then fast | Two execution environments to reason about |

```razor
@rendermode InteractiveServer
@rendermode InteractiveWebAssembly
@rendermode InteractiveAuto
```

Or globally in `App.razor`:

```razor
<Routes @rendermode="InteractiveServer" />
```

## Choosing

- **Static SSR** for content, marketing pages, documentation, and anything a search engine should index cheaply. It is the default, and most of a typical site should stay here.
- **Interactive Server** for internal tools and dashboards: full server access, no API layer needed, no download. Requires a persistent connection, so it is a poor fit for flaky mobile networks.
- **Interactive WebAssembly** for app-like experiences, offline support, and when you want to remove per-user server state. Everything it needs must come from an HTTP API.
- **Interactive Auto** when you want Server's first load and WebAssembly's independence — at the cost of writing code that must run correctly in both.

## Prerendering

Interactive components are prerendered on the server by default: the user sees markup immediately, then the component becomes interactive. Two consequences to design for:

1. `OnInitializedAsync` runs **twice** — once prerendering, once after the component activates. Make it idempotent, or persist the state:

   ```razor
   @implements IDisposable
   @inject PersistentComponentState State
   ```

2. There is no DOM and no `localStorage` during prerender. Guard interop with `OnAfterRenderAsync(firstRender)`.

Disable it per component when the complexity is not worth it: `@rendermode="new InteractiveServerRenderMode(prerender: false)"`.

## Interactive Server in production

Each connected user holds a circuit: server memory, plus a SignalR connection. Plan for it:

- Configure `CircuitOptions.DisconnectedCircuitMaxRetained` and the retention period to bound memory.
- Use sticky sessions or Azure SignalR Service behind a load balancer.
- Handle reconnection in the UI; the default reconnect banner is a starting point, not a finished experience.

## Further reading

- [Blazor render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
