---
name: dotnet-performance
description: Measure and improve .NET performance. Use for benchmarking with BenchmarkDotNet, allocation and GC pressure, Span and Memory, string and JSON hot paths, startup time, Native AOT and trimming.
---

# Performance

Do not optimize from intuition. The order is: **reproduce, measure, change one thing, measure again.** A change without a before-and-after number is a guess, and this skill will not help you defend it in review.

State the goal in numbers before you start — p99 latency, allocations per request, startup time, RSS. "Faster" is not a goal.

## Find the real cost first

Most .NET performance problems are not CPU. In order of how often they are the answer:

1. **N+1 queries or an unindexed query.** Look at the SQL before you look at the C#. See the `ef-core` skill.
2. **Blocking async code** starving the thread pool (`.Result`, `.Wait()`, sync I/O). See `dotnet-async`.
3. **No caching** on an expensive, repeatable read.
4. **Chatty network calls** that should be batched.
5. Only then: allocations, serialization, and algorithmic work.

```bash
dotnet-counters monitor -n <process> System.Runtime   # alloc rate, GC gen2, %time in GC
dotnet-trace collect -n <process> --profile cpu-sampling
dotnet-gcdump collect -n <process>
```

## Benchmark properly

```csharp
[MemoryDiagnoser]
public class ParseBenchmarks
{
    private readonly string _input = File.ReadAllText("sample.csv");

    [Benchmark(Baseline = true)] public int Split() => _input.Split(',').Length;
    [Benchmark] public int Span() => CountSpan(_input);
}
```

```bash
dotnet run -c Release --project Benchmarks   # Release only; never benchmark in Debug or under a debugger
```

- Always `[MemoryDiagnoser]`, always a `[Baseline]`, always Release.
- Return a value from the benchmark so the JIT cannot eliminate the work; keep setup in `[GlobalSetup]`, out of the measured body.
- Compare the mean *and* the allocation column. A 5% mean improvement that doubles allocations is usually a regression under load.
- Micro-benchmarks lie about end-to-end behavior. Confirm the win with a load test (`k6`, `bombardier`, `crank`) against the real endpoint.

## Reduce allocations where it matters

Apply these on measured hot paths only — they cost readability, and everywhere else that trade is a bad one.

| Pattern | Instead of |
| --- | --- |
| `ReadOnlySpan<char>` slicing, `MemoryExtensions` | `Substring`, `Split` in a loop |
| `string.Create` / `ArrayPool<T>.Shared` | intermediate buffers per call |
| `StringBuilder` (or interpolated string handlers) | `+=` in a loop |
| `Utf8JsonReader` / source-generated `JsonSerializerContext` | reflection-based `JsonSerializer` on hot paths |
| `CollectionsMarshal.AsSpan`, `FrozenDictionary` | re-enumerating and re-building lookups |
| `ValueTask` for a method that usually completes synchronously | `Task` allocated every call |
| `struct` enumerators, `foreach` over `List<T>` directly | LINQ chains in a per-request loop |

- Pre-size collections you know the length of: `new List<T>(count)`.
- Cache what is stable: compiled `Regex` (or `[GeneratedRegex]`), `JsonSerializerOptions`, `HttpClient` via `IHttpClientFactory`.
- Never `stackalloc` on an unbounded length, and never return a `Span<T>` over a pooled buffer you have returned to the pool.
- `ValueTask` may be awaited exactly once and never blocked on. If in doubt, use `Task`.

## Server-side wins with no code cost

- Enable output caching / response caching and `ETag`s on cacheable reads; use `IMemoryCache` with an absolute expiry and `HybridCache` when a distributed layer exists.
- Turn on response compression for text payloads.
- `<TieredPGO>` and ReadyToRun for startup-sensitive workloads; Server GC for throughput, Workstation GC for many small containers.
- Cap concurrency at the edge (rate limiting) rather than letting queues grow unbounded.

## Native AOT and trimming

Worth it for CLI tools, functions, and small containers where startup and RSS dominate. Enable early — retrofitting is painful.

```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>true</InvariantGlobalization>
```

Reflection-based serialization, dynamic code, and many ORMs do not survive trimming. Use source generators for JSON and configuration binding, and treat every `IL2xxx`/`IL3xxx` warning as a runtime crash you have not hit yet.

## Complete the change

Report before and after with the same benchmark or load test, including allocations. Say what got worse — readability, memory, startup — and keep the simpler version if the win is inside the noise.
