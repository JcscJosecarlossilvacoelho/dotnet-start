---
name: ef-core
description: Design, implement, or review Entity Framework Core persistence. Use for DbContext modeling, Fluent API mappings, migrations, LINQ query performance, tracking, transactions, concurrency, and relational database tests.
---

# Entity Framework Core

Inspect the EF Core and provider versions, the `DbContext`, the entity configurations, the migrations folder, and the query you are about to change. Preserve the project's migration history and its database naming conventions.

```bash
rg -n 'Microsoft.EntityFrameworkCore' -g '*.csproj'
rg --files -g 'Migrations/*.cs' | sort | tail -5
```

## Model deliberately

- Model the domain first, then express storage details with the **Fluent API** in `IEntityTypeConfiguration<T>` classes, one per entity. Attributes leak persistence concerns into domain types; keep them for trivial cases only.
- Make explicit anything the database must enforce: requiredness, max length, decimal precision, unique indexes, delete behavior, and concurrency tokens. A `decimal` without precision silently truncates money on SQL Server.
- Use owned types / complex types for value objects, and a `readonly record struct` + value converter for strongly typed ids.
- One `DbContext` per unit of work. It is **not** thread-safe — never share it across concurrent awaits or capture it in a singleton.

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(o => o.Id);
        b.Property(o => o.Total).HasPrecision(18, 2);
        b.Property(o => o.Reference).HasMaxLength(32).IsRequired();
        b.HasIndex(o => o.Reference).IsUnique();
        b.Property<uint>("Version").IsRowVersion();          // optimistic concurrency
        b.HasMany(o => o.Lines).WithOne().OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Query deliberately

The two failure modes that matter are **N+1 queries** and **fetching more than you need**. Both are visible in the generated SQL, so look at it.

```csharp
// Good — one query, projected, untracked, cancellable, bounded.
var page = await db.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.PlacedAt)
    .Select(o => new OrderSummary(o.Id, o.Reference, o.Total, o.Lines.Count))
    .Take(50)
    .ToListAsync(ct);

// Avoid — loads whole entities and graphs, then filters in memory.
var all = await db.Orders.Include(o => o.Lines).ToListAsync(ct);
var mine = all.Where(o => o.CustomerId == customerId).ToList();
```

- `AsNoTracking()` on every read-only query. Tracking exists to save changes; paying for it on reads is pure cost.
- Project with `Select` into a DTO instead of `Include`-ing a graph you will not fully use. Use `AsSplitQuery()` when a legitimate `Include` produces a cartesian explosion.
- Always the async terminal operator with a `CancellationToken`: `ToListAsync(ct)`, `FirstOrDefaultAsync(ct)`, `AnyAsync(ct)`.
- Never let a client control an unbounded result set. Paginate, and prefer keyset pagination over large `Skip`.
- Anything after `AsEnumerable()`, `ToList()`, or a method EF cannot translate runs **in memory**. That is where accidental full-table scans come from.
- Do not call `await` inside a `foreach` over entities to load related data — that *is* the N+1.

```csharp
// Inspect a query without executing it.
var sql = query.ToQueryString();

// Or log executed commands in development:
options.UseNpgsql(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information);
```

Add `EnableSensitiveDataLogging()` only when parameter values are essential to a local diagnosis; it can expose credentials and personal data.

## Save deliberately

- `SaveChangesAsync` is already a transaction. Open an explicit `BeginTransactionAsync` only when one atomic operation spans several `SaveChanges` calls or coordinates non-EF work.
- Handle `DbUpdateConcurrencyException` where the user can act on it; a concurrency token with no handler is just a 500.
- Translate unique-constraint violations into a 409, not an unhandled exception.
- For bulk operations use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` — they issue one statement and bypass the change tracker (and therefore also bypass interceptors and `SaveChanges` logic, so check what you lose).
- Do not put domain side effects in `SaveChanges` overrides unless the repository already does; if it does, follow it.

## Migrations

```bash
dotnet ef migrations add AddOrderReference
dotnet ef migrations script <from> <to> -o migration.sql   # review this
dotnet ef database update
```

- **Read every generated migration before applying it.** EF will happily generate a destructive column drop from an innocent-looking model edit.
- Call out anything destructive or locking: dropped columns, type narrowing, an index build on a large table, a `NOT NULL` added without a default.
- Split a rename into expand → backfill → contract across releases when the old and new code must run at once.
- Keep data backfills out of schema migrations when a staged rollout is safer.
- Never edit an applied migration; add a new one.
- `EnsureCreated` is for throwaway scenarios only — it does not compose with migrations.

## Test against a real database

Use Testcontainers (or a compatible local instance) so constraints, transactions, and provider-specific SQL are actually exercised. The in-memory provider enforces nothing and will let a broken query pass. See the `dotnet-testing` skill.

## Complete the change

```bash
dotnet build && dotnet test
```

Report the generated SQL for any query you changed on a performance-sensitive path, and state explicitly whether the migration is safe to run against production data while the previous version is still serving traffic.
