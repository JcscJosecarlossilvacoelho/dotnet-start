---
title: Saving, transactions, and concurrency
description: How SaveChanges works, when you need an explicit transaction, and how to handle concurrent edits.
order: 50
---

## SaveChanges is already a transaction

`SaveChangesAsync` wraps every statement it generates in one transaction. You need an explicit transaction only when a unit of work spans several calls to `SaveChanges` or mixes EF with raw SQL:

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);
try
{
    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);

    await db.Database.ExecuteSqlAsync($"UPDATE stock SET quantity = quantity - {order.Quantity} WHERE sku = {order.Sku}", ct);

    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

Keep transactions short. A transaction held open across an HTTP call to another service is a lock held for the duration of someone else's outage.

## Optimistic concurrency

Add a concurrency token and let the database detect conflicting writes:

```csharp
builder.Property(o => o.RowVersion).IsRowVersion();          // SQL Server
builder.Property(o => o.Version).IsConcurrencyToken();       // portable alternative
```

```csharp
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var current = await entry.GetDatabaseValuesAsync(ct);
    if (current is null) return Results.NotFound();          // deleted by someone else

    return Results.Conflict(new { message = "The order changed while you were editing it." });
}
```

Optimistic concurrency is the default choice for web applications: no locks held between requests, conflicts detected at write time. Reserve pessimistic locking (`SELECT ... FOR UPDATE`) for short, contended, server-side operations.

## Isolation levels

| Level | Prevents | Cost |
| --- | --- | --- |
| Read committed (default in most engines) | Dirty reads | Low |
| Repeatable read | Non-repeatable reads | Medium |
| Serializable | Phantoms; full isolation | Highest; expect retries |
| Snapshot | Readers do not block writers | Version-store overhead |

Choose consciously when correctness depends on it, and be ready to retry serialization failures.

## Bulk operations

`SaveChanges` is row-by-row. For bulk changes, skip the change tracker entirely:

```csharp
await db.Orders
    .Where(o => o.Status == OrderStatus.Draft && o.Placed < cutoff)
    .ExecuteDeleteAsync(ct);

await db.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Expired), ct);
```

These generate a single statement. They do **not** update entities already tracked in memory, and they do not raise `SaveChanges` interceptors — use them for maintenance operations, not for domain logic that expects events.

## Retrying transient failures

```csharp
options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3));
```

With a retrying execution strategy, an explicit transaction must be wrapped so the whole unit is retried together:

```csharp
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () => { /* transaction here */ });
```

## Further reading

- [Saving data](https://learn.microsoft.com/ef/core/saving/)
- [Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)
