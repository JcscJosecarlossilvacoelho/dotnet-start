---
title: Garbage collection
description: Generations, the large object heap, server vs workstation mode, and how to read GC pressure.
order: 10
---

The GC is a tracing, generational, compacting collector. You allocate; it decides when to reclaim. Understanding its model tells you which code patterns are cheap and which are not.

## Generations

| Generation | Contains | Collection cost |
| --- | --- | --- |
| Gen 0 | Freshly allocated objects | Very cheap, very frequent |
| Gen 1 | Gen 0 survivors — a buffer between short and long lived | Cheap |
| Gen 2 | Long-lived objects | Expensive; touches the whole heap |
| LOH | Objects ≥ 85,000 bytes | Collected with gen 2; not compacted by default |
| POH | Pinned objects | Kept separate to avoid fragmenting the rest |

The generational hypothesis holds for most services: most objects die young. Allocating a short-lived object is close to free — a pointer bump — and collecting it costs almost nothing. **Allocation is not the problem; survival is.**

## What that means for your code

- Short-lived request objects: fine. Do not contort code to avoid them.
- Objects that survive into gen 2 by accident — caches without limits, static collections, event handlers never unsubscribed — are what cause long pauses.
- Large arrays and buffers go to the LOH. Pool them (`ArrayPool<T>`) instead of allocating repeatedly.
- A `struct` avoids allocation only while it stays unboxed; putting it in an `object`, a non-generic collection, or an interface variable boxes it.

## Server vs workstation GC

```xml
<PropertyGroup>
  <ServerGarbageCollection>true</ServerGarbageCollection>
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
</PropertyGroup>
```

Server GC uses one heap and one collection thread per core: higher throughput, more memory. It is the default for ASP.NET Core. In a small container it can be counterproductive — with a CPU limit below one core, or a tight memory limit, workstation GC often behaves better. Set `DOTNET_GCHeapHardLimitPercent` when the container limit should bound the heap.

## Finalizers and disposal

A finalizer delays reclamation by at least one collection cycle and runs on a separate thread with no ordering guarantees. Implement `IDisposable` for deterministic cleanup; add a finalizer **only** when the type directly owns an unmanaged handle — and prefer `SafeHandle`, which already does it correctly.

## Reading GC behaviour

```bash
dotnet-counters monitor --process-id <pid> System.Runtime
```

Watch: gen 2 collection count (should be rare), `% Time in GC` (a few per cent is normal), allocation rate, and heap size trend. A heap that grows monotonically across gen 2 collections is a leak — capture a dump:

```bash
dotnet-gcdump collect -p <pid>
```

and compare two dumps to find which type is retaining memory, and what is holding it.

## Further reading

- [Fundamentals of garbage collection](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals)
- [Runtime configuration options for GC](https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector)
