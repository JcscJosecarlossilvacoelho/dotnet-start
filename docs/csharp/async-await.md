---
title: Async and await
description: What the compiler generates, what a Task really is, and the rules that keep async code correct.
order: 50
---

`async`/`await` is about **not blocking a thread while waiting**. It is not about parallelism: an async method runs on one thread at a time.

## What the compiler generates

An `async` method is rewritten into a state machine. `await` splits the method at that point: the remainder becomes a continuation registered on the awaited operation. When the operation completes, the continuation is scheduled and the method resumes with its locals intact.

Consequences that matter in practice:

- The method returns to its caller at the first `await` that is not already complete.
- Locals survive across awaits because they are fields of the generated state machine.
- An async method that never awaits runs entirely synchronously (and warns: CS1998).

## Task, ValueTask, and friends

| Type | Use |
| --- | --- |
| `Task` | An operation with no result |
| `Task<T>` | An operation producing a `T` |
| `ValueTask<T>` | Hot paths that usually complete synchronously (cache hits); may only be awaited once |
| `IAsyncEnumerable<T>` | A stream of results consumed with `await foreach` |
| `Task.CompletedTask` | Returning without allocating |

## The rules

**Async all the way.** Never call `.Result` or `.Wait()` on a task from application code: it blocks a thread and can deadlock in contexts with a synchronisation context. Make the caller async instead.

**Take a `CancellationToken` and pass it on.** Every async method that does IO should accept one and hand it to everything it calls.

```csharp
public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken)
    => await db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
```

**Do not use `async void`.** Exceptions escape to the runtime and crash the process. The single exception is an event handler, which should have a try/catch around its whole body.

**Do not fire and forget.** An unawaited task loses its exceptions and may be cut short at shutdown. Use a [background service](/docs/ops/background-services) or a queue for work that outlives the request.

## Running work concurrently

```csharp
var customerTask = GetCustomerAsync(id, ct);
var ordersTask   = GetOrdersAsync(id, ct);
await Task.WhenAll(customerTask, ordersTask);

var customer = await customerTask;
var orders   = await ordersTask;
```

Start both, then await both. `Task.WhenAll` rethrows the first exception; inspect `Task.Exception` on each task if you need them all. For bounded fan-out, use `Parallel.ForEachAsync` with `MaxDegreeOfParallelism`.

## ConfigureAwait

In ASP.NET Core there is no synchronisation context, so `ConfigureAwait(false)` changes nothing in application code. In **libraries**, keep using it: your code may be consumed by a UI application where resuming on the captured context is expensive or deadlock-prone.

## Common failures

| Symptom | Cause |
| --- | --- |
| Deadlock under load | `.Result`/`.Wait()` on a blocking call path |
| Thread-pool starvation | Sync-over-async or long CPU work on pool threads |
| Exception lost | `async void` or an unawaited task |
| `ObjectDisposedException` on `DbContext` | A fire-and-forget task outliving the request scope |

## Further reading

- [Async programming](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
- [Async guidance (David Fowler)](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)
