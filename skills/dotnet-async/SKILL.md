---
name: dotnet-async
description: Write or fix asynchronous C#. Use for async/await correctness, cancellation, deadlocks, parallelism, thread safety, channels, background services, and diagnosing hangs or thread-pool starvation.
---

# Async .NET

Most async bugs are not subtle: blocking on a task, dropping a `CancellationToken`, or making a fire-and-forget call that swallows its exception. Check for those three before anything else.

## The rules that prevent hangs

- **Async all the way.** Never call `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on a task in application code. In a request pipeline that is a deadlock or a starved thread pool, not a shortcut.
- **Never `async void`.** The only exception is an event handler, which must then wrap its whole body in try/catch — an exception from `async void` crashes the process.
- **Return the task** when you have nothing to do after the await (`return inner.DoAsync(ct);`), except inside `using`/`try`, where you must `await` so the scope survives.
- **`ConfigureAwait(false)`** on every await in library code. Application code on ASP.NET Core does not need it (there is no synchronization context), and Blazor components must *not* use it — they need the renderer's context.

```csharp
// Avoid — blocks a thread-pool thread and can deadlock.
var user = _users.GetAsync(id).Result;

// Good
var user = await _users.GetAsync(id, cancellationToken);
```

## Cancellation is part of the signature

- Every async method that does I/O takes `CancellationToken cancellationToken` as its last parameter and passes it down. A token you accept and do not forward is a bug.
- ASP.NET Core hands you `HttpContext.RequestAborted`; Minimal APIs and controllers bind a `CancellationToken` parameter to it automatically.
- Let `OperationCanceledException` propagate. Do not log it as an error, and do not convert it to a 500.
- Compose deadlines with `CancellationTokenSource.CreateLinkedTokenSource(ct)` plus `CancelAfter`, and dispose the source.

```csharp
app.MapGet("/orders/{id}", async (int id, IOrderStore store, CancellationToken ct) =>
    await store.FindAsync(id, ct) is { } order ? Results.Ok(order) : Results.NotFound());
```

## Concurrency

| Situation | Use |
| --- | --- |
| Several independent I/O calls | `await Task.WhenAll(a, b, c)` |
| Bounded concurrency over a collection | `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` |
| CPU-bound work off the request thread | `Task.Run` — once, at the call site, never inside a library |
| Producer/consumer pipeline | `System.Threading.Channels` |
| Streaming results to a caller | `IAsyncEnumerable<T>` with `[EnumeratorCancellation]` |
| Guarding an async critical section | `SemaphoreSlim.WaitAsync` — `lock` cannot be awaited |

`Task.WhenAll` reports only the first exception when awaited; inspect `task.Exception.InnerExceptions` when every failure matters.

```csharp
// Good — bounded, cancellable fan-out.
await Parallel.ForEachAsync(
    ids,
    new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (id, token) => await ProcessAsync(id, token));
```

## Thread safety

- A `DbContext`, `HttpContext`, and most `System.Text.Json` writers are single-threaded. Never share one across concurrent awaits.
- Do not capture scoped services in a singleton. In a `BackgroundService`, create a scope per unit of work with `IServiceScopeFactory`.
- Prefer immutable state and `ConcurrentDictionary`/`Interlocked` over hand-rolled locking. Never `await` inside a `lock`.

## Background work

```csharp
public sealed class OutboxWorker(IServiceScopeFactory scopes, ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OutboxPump>().PumpAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox pump failed; retrying next tick");
            }
        }
    }
}
```

An unhandled exception in `ExecuteAsync` stops the host by default. Catch inside the loop so one bad iteration does not end the service. Never start work in an ASP.NET Core request that outlives the response without a durable queue behind it.

## Diagnose a hang

```bash
dotnet-counters monitor -n <process> System.Runtime Microsoft.AspNetCore.Hosting
dotnet-stack report -n <process>       # who is blocked, and on what
dotnet-dump collect -n <process>       # then: dumpasync in dotnet-dump analyze
```

Rising `ThreadPool Queue Length` with low CPU means blocking on async work — look for `.Result`, `.Wait()`, and synchronous I/O on the request path.

## Complete the change

Build and run the tests. For anything concurrent, add a test that actually cancels: pass an already-cancelled token and assert `OperationCanceledException`, and assert that a slow dependency does not outlive its deadline.
