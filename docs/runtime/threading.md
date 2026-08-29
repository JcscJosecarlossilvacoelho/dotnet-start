---
title: Threads, tasks, and the thread pool
description: What actually runs your code, how to parallelise safely, and how to avoid starving the pool.
order: 20
---

## The thread pool

Almost all .NET code runs on pool threads: request handlers, task continuations, timer callbacks. The pool starts with a thread per core and injects more slowly (roughly one or two per second) when work queues up. That injection rate is why **blocking a pool thread is expensive** — the pool cannot instantly replace it, and latency spikes across the whole process.

Two rules follow:

1. Never block on IO (`.Result`, `.Wait()`, `Thread.Sleep`) inside pool work.
2. Do not run long CPU-bound loops on the pool without bounding them — use `Parallel` APIs with a degree of parallelism, or a dedicated long-running thread.

## Concurrency vs parallelism

- **Concurrency** — many operations in flight, mostly waiting: use `async`/`await`. See [Async and await](/docs/csharp/async-await).
- **Parallelism** — one job split across cores: use `Parallel.For`, `Parallel.ForEachAsync`, or PLINQ.

```csharp
await Parallel.ForEachAsync(
    urls,
    new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (url, token) => await ProcessAsync(url, token));
```

Unbounded fan-out over a remote service is a denial-of-service attack on your own dependency. Always set a limit.

## Sharing state safely

| Tool | Use |
| --- | --- |
| `Interlocked` | Atomic counters and compare-and-swap |
| `lock` | Short critical sections, no `await` inside |
| `SemaphoreSlim` | Async-safe mutual exclusion (`await WaitAsync`) |
| `ConcurrentDictionary<,>` | Shared lookup tables |
| `Channel<T>` | Producer/consumer pipelines with back-pressure |
| `Lazy<T>` | Thread-safe one-time initialisation |
| `Immutable*` | Snapshot semantics; readers never lock |

You cannot `await` inside a `lock`. When a critical section needs async work, use `SemaphoreSlim`:

```csharp
await _gate.WaitAsync(ct);
try { await RefreshAsync(ct); }
finally { _gate.Release(); }
```

## Channels

`System.Threading.Channels` is the right primitive for an in-process queue:

```csharp
var channel = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.Wait
});

await channel.Writer.WriteAsync(item, ct);          // producer, awaits when full

await foreach (var item in channel.Reader.ReadAllAsync(ct))   // consumer
    await HandleAsync(item, ct);
```

Bounded channels give you back-pressure: when consumers fall behind, producers slow down instead of the process running out of memory.

## Diagnosing starvation

Symptoms: latency climbs while CPU stays low; `ThreadPool` queue length grows.

```bash
dotnet-counters monitor -p <pid> System.Runtime[threadpool-queue-length,threadpool-thread-count]
dotnet-stack report -p <pid>     # what are the threads actually doing?
```

Almost every case traces back to sync-over-async somewhere on the path.

## Further reading

- [Managed threading best practices](https://learn.microsoft.com/dotnet/standard/threading/managed-threading-best-practices)
- [System.Threading.Channels](https://learn.microsoft.com/dotnet/core/extensions/channels)
