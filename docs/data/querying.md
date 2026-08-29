---
title: Querying
description: Writing LINQ that translates to good SQL — projections, includes, split queries, and the N+1 problem.
order: 40
---

## Project to what you need

The single highest-impact habit in EF Core:

```csharp
var summaries = await db.Orders
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.Placed)
    .Select(o => new OrderSummary(o.Id, o.Reference, o.Total, o.Lines.Count))
    .AsNoTracking()
    .ToListAsync(ct);
```

A projection selects only the columns it uses, needs no change tracking, and cannot trigger lazy loads. Load full entities when you intend to **modify** them; project for everything else.

## Loading related data

```csharp
var order = await db.Orders
    .Include(o => o.Lines)
        .ThenInclude(l => l.Product)
    .FirstOrDefaultAsync(o => o.Id == id, ct);
```

Multiple `Include`s over collections produce a cartesian product. When the row count explodes, split the query:

```csharp
.AsSplitQuery()      // one SELECT per collection, joined in memory
```

Split queries trade round trips for row volume, and they are not atomic unless wrapped in a transaction — usually the right trade for read-heavy pages.

## The N+1 problem

```csharp
var orders = await db.Orders.ToListAsync(ct);
foreach (var order in orders)
    Console.WriteLine(order.Customer.Name);   // one query per order
```

Fix it with an `Include` or a projection. Detect it by logging SQL in development:

```csharp
options.UseNpgsql(cs).LogTo(Console.WriteLine, LogLevel.Information).EnableSensitiveDataLogging();
```

`EnableSensitiveDataLogging` prints parameter values — development only, never in production.

## Filtering, paging, and counting

```csharp
var page = await db.Orders
    .Where(o => o.Status == status)
    .OrderBy(o => o.Placed).ThenBy(o => o.Id)      // a stable, total ordering
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);
```

Paging without a deterministic `OrderBy` returns arbitrary results. For deep paging, prefer keyset pagination (`WHERE (placed, id) > (@lastPlaced, @lastId)`) — `Skip` gets linearly slower as the offset grows.

## What cannot be translated

If EF cannot translate an expression it throws. That is a feature: the alternative is silently fetching the table. Compute in SQL what SQL can do, and only then move to memory:

```csharp
var rows = await db.Orders.Where(o => o.Placed >= from).ToListAsync(ct);
var grouped = rows.GroupBy(o => Categorise(o));       // C# method: must run client-side
```

## Global query filters

```csharp
builder.HasQueryFilter(o => o.TenantId == _tenant.Current && !o.IsDeleted);
```

Applied to every query for that entity, which makes them the right enforcement point for soft delete and multi-tenancy. Bypass explicitly with `IgnoreQueryFilters()` when an admin path genuinely needs to.

## Reading the SQL

```csharp
var sql = db.Orders.Where(o => o.Total > 100).ToQueryString();
```

Then run it with `EXPLAIN`. An index that does not exist is a far more common cause of slowness than anything in the C#.

## Further reading

- [Querying data](https://learn.microsoft.com/ef/core/querying/)
- [Efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying)
