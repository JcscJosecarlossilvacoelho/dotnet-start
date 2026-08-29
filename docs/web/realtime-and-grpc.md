---
title: Real-time and RPC
description: SignalR for pushing to clients, gRPC for service-to-service calls, and SSE for simple streams.
order: 180
---

## Choosing a transport

| Need | Use |
| --- | --- |
| Push updates to browsers, with reconnection and fallbacks | SignalR |
| One-way server-to-client stream, plain HTTP | Server-sent events |
| Typed, high-throughput service-to-service calls | gRPC |
| Public API for third parties | HTTP + JSON |

## SignalR

```csharp
builder.Services.AddSignalR();

app.MapHub<OrderHub>("/hubs/orders");

public sealed class OrderHub : Hub
{
    public Task Subscribe(string tenantId) => Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
}
```

Push from anywhere in the application through the hub context:

```csharp
public sealed class OrderNotifier(IHubContext<OrderHub> hub)
{
    public Task PublishAsync(Order order, CancellationToken ct) =>
        hub.Clients.Group(order.TenantId).SendAsync("orderUpdated", order, ct);
}
```

Operational notes:

- Connections are **sticky**. Scaling out requires a backplane (Redis) or Azure SignalR Service, otherwise a message published on one instance never reaches clients connected to another.
- Authorize hubs and hub methods exactly like endpoints — `[Authorize]` works on both.
- Assume disconnections. Clients must resubscribe and reconcile state after reconnecting.
- Never send large payloads over a hub; send an event and let the client fetch.

## Server-sent events

When you only need server → client text, SSE needs no library on either side:

```csharp
app.MapGet("/stream", async (HttpContext context, CancellationToken ct) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    while (!ct.IsCancellationRequested)
    {
        await context.Response.WriteAsync($"data: {DateTimeOffset.UtcNow:O}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
    }
});
```

## gRPC

Define the contract once, generate both sides:

```proto
service Orders {
  rpc Get (GetOrderRequest) returns (OrderReply);
  rpc Watch (WatchRequest) returns (stream OrderReply);
}
```

```xml
<ItemGroup>
  <Protobuf Include="Protos/orders.proto" GrpcServices="Server" />
</ItemGroup>
```

gRPC gives you a typed contract, binary framing, and streaming in both directions over HTTP/2. Its costs: browsers need grpc-web or a gateway, the payloads are not human-readable, and every consumer needs the `.proto`. Use it between your own services; expose HTTP + JSON at the edge.

## Further reading

- [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction)
- [gRPC on .NET](https://learn.microsoft.com/aspnet/core/grpc/)
