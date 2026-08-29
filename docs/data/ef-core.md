---
title: EF Core essentials
description: What the DbContext is, how change tracking works, and the mental model that prevents most EF surprises.
order: 10
---

EF Core is an object-relational mapper: it turns LINQ into SQL and rows into objects, and tracks what you changed so it can write it back.

## The DbContext

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
        => builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
```

A `DbContext` is a **unit of work**: it is scoped, short-lived, and **not thread-safe**. One per request, never shared across concurrent operations. Two `await`s on the same context in parallel is the single most common EF Core bug.

## Change tracking

Entities loaded by a tracking query are watched. `SaveChangesAsync` inspects them, generates the INSERT/UPDATE/DELETE statements, and runs them in one transaction.

```csharp
var order = await db.Orders.FirstAsync(o => o.Id == id, ct);
order.Status = OrderStatus.Shipped;      // no SQL yet
await db.SaveChangesAsync(ct);           // one UPDATE, inside a transaction
```

For read-only work, opt out — no snapshots, less memory, faster:

```csharp
var orders = await db.Orders.AsNoTracking().Where(o => o.Placed >= from).ToListAsync(ct);
```

## The three things that surprise people

1. **A query executes when you enumerate it**, not when you write it. `ToListAsync`, `FirstAsync`, `AnyAsync`, `CountAsync`, and `await foreach` are the trigger points.
2. **Lazy loading is off by default**, and should stay off. Load what you need with `Include` or a projection; see [Querying](/docs/data/querying).
3. **The model is built once per application**, from `OnModelCreating`. Conditional logic based on runtime state does not belong there.

## Pooling the context

```csharp
builder.Services.AddDbContextPool<AppDbContext>(options => options.UseNpgsql(connectionString));
```

Pooling reuses context instances and measurably reduces allocation in high-throughput services. The constraint: the context must not hold per-request state of its own beyond what EF resets.

## Where to use EF Core, and where not

EF Core is excellent for domain-shaped reads and writes, migrations, and change tracking. For bulk operations, complex reporting queries, and anything where you want to control the exact plan, use `ExecuteUpdate`/`ExecuteDelete` or drop to [raw SQL](/docs/data/dapper-and-ado). Mixing the two in one application is normal engineering, not a failure of the ORM.

## Further reading

- [EF Core documentation](https://learn.microsoft.com/ef/core/)
