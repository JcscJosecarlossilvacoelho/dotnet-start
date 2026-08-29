---
title: Collections and LINQ
description: Choosing the right collection type, and using LINQ without paying for it twice.
order: 40
---

## Choosing a collection

| Need | Type | Notes |
| --- | --- | --- |
| Ordered, growable list | `List<T>` | The default; `[]` initialises it |
| Fixed-size, hot loop | `T[]` | Lowest overhead, indexable |
| Key lookup | `Dictionary<TKey,TValue>` | O(1) average; needs good `GetHashCode` |
| Uniqueness | `HashSet<T>` | Set operations built in |
| Ordered by key | `SortedDictionary<TKey,TValue>` | O(log n), iterates in key order |
| FIFO / LIFO | `Queue<T>` / `Stack<T>` | |
| Shared across threads | `ConcurrentDictionary<TKey,TValue>` | See [threading](/docs/runtime/threading) |
| Never mutated after build | `ImmutableArray<T>`, `FrozenDictionary<TKey,TValue>` | `Frozen*` optimises for read-heavy lookups |
| Return type of an API | `IReadOnlyList<T>`, `IReadOnlyDictionary<,>` | Do not hand out your mutable field |

Collection expressions work across all of them:

```csharp
List<int> numbers = [1, 2, 3];
int[] copy = [..numbers, 4];
ReadOnlySpan<char> letters = ['a', 'b'];
```

## LINQ is lazy

`Where`, `Select`, `OrderBy`, and friends build a query; nothing runs until the sequence is enumerated. Two consequences:

```csharp
var query = orders.Where(o => o.Total > 100);   // nothing executed yet
var count = query.Count();                       // enumerates once
var list  = query.ToList();                      // enumerates again
```

Materialise once with `ToList()`/`ToArray()` when you will use the result more than once, and *do not* materialise when you will use it once — you would be allocating a list for nothing.

## Operators worth knowing

```csharp
orders.GroupBy(o => o.CustomerId)
      .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.Total) });

orders.Chunk(500);                       // batching
orders.DistinctBy(o => o.CustomerId);    // no custom comparer needed
orders.OrderBy(o => o.Placed).ThenByDescending(o => o.Total);
orders.Aggregate(0m, (sum, o) => sum + o.Total);
```

`FirstOrDefault`, `SingleOrDefault`, and `Any` differ in intent and cost: `Any()` stops at the first match, `Single()` scans on to prove uniqueness. Choose the one that states what you mean.

## LINQ over a database is not LINQ over memory

With EF Core, the expression tree is translated to SQL. Calling a method the provider cannot translate throws, or — worse, in older patterns — silently pulls the table into memory. See [Querying with EF Core](/docs/data/querying).

## Performance notes

- LINQ allocates: an enumerator per operator, plus closures for captured variables. In a hot path called millions of times, a `for` loop over a `Span<T>` is measurably faster.
- In ordinary application code — request handling, business logic — the clarity is worth far more than the allocation.
- Measure with [BenchmarkDotNet](/docs/runtime/benchmarking) before rewriting readable LINQ into loops.

## Further reading

- [LINQ overview](https://learn.microsoft.com/dotnet/csharp/linq/)
- [Collections and data structures](https://learn.microsoft.com/dotnet/standard/collections/)
