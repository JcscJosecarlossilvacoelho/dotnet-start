---
title: Data access performance
description: Where the time actually goes, and the small set of changes that fix most of it.
order: 60
---

Most "EF Core is slow" reports are one of five things. Check them in this order.

## 1. A missing index

Get the SQL, run `EXPLAIN` (or the equivalent), and look for a sequential scan on a large table:

```csharp
logger.LogInformation("{Sql}", query.ToQueryString());
```

Declare the index in the [model](/docs/data/modeling) so it ships with the migration.

## 2. Fetching too much

- `Select` a projection instead of loading whole entities.
- `AsNoTracking()` on every read-only query — or set it as the default:

  ```csharp
  options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
  ```

- Page results; never return an unbounded list to a client.

## 3. Too many round trips

The N+1 pattern, chatty loops calling `SaveChanges` per item, and per-item lookups. Batch instead:

```csharp
var products = await db.Products
    .Where(p => skus.Contains(p.Sku))
    .ToDictionaryAsync(p => p.Sku, ct);
```

EF batches multiple inserts/updates into one round trip automatically — one `SaveChanges` at the end of the unit of work beats one per entity.

## 4. Query compilation overhead

Every query is translated and cached by shape. Two things break the cache: dynamically built expression trees, and `EnableSensitiveDataLogging` style constant inlining. For very hot queries, compile once:

```csharp
private static readonly Func<AppDbContext, Guid, CancellationToken, Task<Order?>> GetOrder =
    EF.CompileAsyncQuery((AppDbContext db, Guid id, CancellationToken ct) =>
        db.Orders.FirstOrDefault(o => o.Id == id));
```

## 5. Connection pool pressure

Symptoms: timeouts acquiring a connection, latency that grows with concurrency. Causes: transactions held too long, `DbContext` instances leaking out of their scope, or a pool size lower than the concurrency you actually serve. Watch the `Microsoft.EntityFrameworkCore` and provider counters with [`dotnet-counters`](/docs/runtime/diagnostics).

## Measuring rather than guessing

- Log commands slower than a threshold in production, with the parameters redacted.
- Emit EF Core's OpenTelemetry instrumentation so database spans appear in your [traces](/docs/ops/observability) next to the request they belong to.
- Compare candidate query shapes with [BenchmarkDotNet](/docs/runtime/benchmarking) against a realistic dataset, not five rows.

## When to leave EF

Reporting queries with heavy aggregation, bulk imports, and anything needing a specific plan are better written as SQL. Keep them in a repository behind a method name so the rest of the code does not care — see [Raw SQL and Dapper](/docs/data/dapper-and-ado).

## Further reading

- [EF Core performance](https://learn.microsoft.com/ef/core/performance/)
