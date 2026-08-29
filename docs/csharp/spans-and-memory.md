---
title: Span, Memory, and allocation-free code
description: The types that let you slice buffers without copying, and when that actually matters.
order: 70
---

`Span<T>` is a view over contiguous memory — an array, a stack buffer, a slice of a string, or unmanaged memory — with no copy and no allocation.

## The family

| Type | Lives where | Notes |
| --- | --- | --- |
| `Span<T>` | Stack only (`ref struct`) | Cannot be a field of a class, cannot cross an `await` |
| `ReadOnlySpan<T>` | Stack only | The right parameter type for read-only buffers |
| `Memory<T>` / `ReadOnlyMemory<T>` | Heap-safe | Can be stored and used in async code; `.Span` to slice |
| `ArraySegment<T>` | Legacy | Prefer the above |

## Slicing without allocating

```csharp
ReadOnlySpan<char> line = "2026-08-29,142.50,EUR";

var firstComma  = line.IndexOf(',');
var date        = line[..firstComma];
var rest        = line[(firstComma + 1)..];
var secondComma = rest.IndexOf(',');

var amount   = decimal.Parse(rest[..secondComma], CultureInfo.InvariantCulture);
var currency = rest[(secondComma + 1)..];
```

`Substring` would have allocated three strings; this allocates none.

## Stack buffers

```csharp
Span<char> buffer = stackalloc char[64];
if (value.TryFormat(buffer, out var written))
    Console.Out.Write(buffer[..written]);
```

Keep `stackalloc` sizes small and constant (a few hundred bytes). For larger or variable sizes, rent from a pool:

```csharp
var buffer = ArrayPool<byte>.Shared.Rent(size);
try { /* use buffer.AsSpan(0, size) */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

Renting and forgetting to return is a leak; returning twice is a corruption. Wrap pooling in a small type when it appears more than once.

## Rules that trip people up

- A `ref struct` cannot be captured by a lambda, stored in a field, boxed, or held across an `await`. Split the async work: read into a `Memory<T>`, then parse from `.Span` in a synchronous helper.
- `Span<T>` over a `string` is always read-only — strings are immutable.
- Slicing does not copy, so the underlying buffer stays alive as long as any `Memory<T>` refers to it.

## When to reach for this

Parsers, serializers, protocol handling, and hot loops that process megabytes. In a typical request handler doing IO and a database round trip, span-based micro-optimisation is invisible next to the network. Profile first — see [Benchmarking](/docs/runtime/benchmarking).

## Further reading

- [Memory and span usage guidelines](https://learn.microsoft.com/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)
