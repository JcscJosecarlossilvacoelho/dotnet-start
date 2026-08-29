---
title: Everyday idioms
description: The small, modern forms that make C# read well — and the older ones they replace.
order: 100
---

None of these change what a program does. Together they decide whether the next person can read it.

## File and type declarations

```csharp
namespace MyApp.Orders;                    // file-scoped, one level of indentation saved

public sealed class OrderService(IOrderRepository repository, TimeProvider time)
{
    public async Task<Order> PlaceAsync(OrderRequest request, CancellationToken ct)
        => await repository.AddAsync(request.ToOrder(time.GetUtcNow()), ct);
}
```

Primary constructors remove the field/assignment ceremony. `TimeProvider` instead of `DateTime.UtcNow` makes time injectable — and therefore testable.

## Initialising and copying

```csharp
List<string> names = ["ana", "rui"];
string[] all = [..names, "sofia"];

var updated = order with { Status = OrderStatus.Shipped };
```

## Strings

```csharp
var summary = $"{order.Id}: {order.Total:C}";                 // interpolation
var query = $$"""
    { "id": "{{order.Id}}" }
    """;                                                       // raw string, braces escaped
var slug = string.Join('-', parts);
if (string.IsNullOrWhiteSpace(input)) return;
```

Use `StringBuilder` when concatenating in a loop, and always pass `CultureInfo.InvariantCulture` when formatting for machines rather than humans.

## Guard clauses over nesting

```csharp
if (order is null) return NotFound();
if (order.Status is OrderStatus.Cancelled) return Conflict();

return Ok(order);
```

Return early. Deeply nested `if` blocks hide the one line that matters.

## Disposal

```csharp
await using var connection = new SqlConnection(connectionString);
using var scope = _logger.BeginScope("Order {OrderId}", id);
```

`using` declarations (no braces) dispose at the end of the enclosing scope. Use `await using` for `IAsyncDisposable`.

## `var`, deliberately

Use `var` when the type is obvious from the right-hand side; write the type when it is not. The goal is a reader who does not need to hover.

## What to stop writing

| Old | Now |
| --- | --- |
| `if (x == null) throw new ArgumentNullException(...)` | `ArgumentNullException.ThrowIfNull(x)` |
| `new List<int>() { 1, 2 }` | `[1, 2]` |
| `DateTime.UtcNow` in a service | `TimeProvider` |
| `#region` | Smaller types |
| `_field` assigned in a long constructor | Primary constructor parameters |
| `Task.Run` around synchronous IO | A real async API |

## Further reading

- [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
