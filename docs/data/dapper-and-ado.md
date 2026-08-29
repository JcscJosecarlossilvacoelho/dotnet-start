---
title: Raw SQL, Dapper, and ADO.NET
description: Dropping below the ORM safely — parameters, mapping, and where each tool fits.
order: 70
---

## Raw SQL inside EF Core

You do not need another library to run SQL. EF Core's interpolated helpers parameterise everything automatically:

```csharp
var orders = await db.Orders
    .FromSql($"SELECT * FROM orders WHERE placed >= {from} AND tenant_id = {tenantId}")
    .AsNoTracking()
    .ToListAsync(ct);

var affected = await db.Database.ExecuteSqlAsync(
    $"UPDATE orders SET status = 'expired' WHERE placed < {cutoff}", ct);
```

`FromSql` and `ExecuteSql` take a `FormattableString`: the interpolated holes become SQL parameters, not string concatenation. `FromSqlRaw` does not — use it only with a constant string, never with user input.

## Dapper

Dapper maps query results to objects and does nothing else, which is exactly why it is good at reporting queries:

```csharp
await using var connection = new NpgsqlConnection(connectionString);

var rows = await connection.QueryAsync<RevenueByMonth>(
    """
    SELECT date_trunc('month', placed) AS month, SUM(total) AS revenue
    FROM orders
    WHERE tenant_id = @tenantId AND placed >= @from
    GROUP BY 1
    ORDER BY 1
    """,
    new { tenantId, from });
```

Named parameters come from an anonymous object; there is no change tracking, no model, and no migration story — bring your own via EF Core.

## Mixing the two

A common, pragmatic split:

- **EF Core** owns the schema, migrations, and all writes.
- **Dapper or raw SQL** serves complex reads and reports.

Share the connection when they must be in the same transaction:

```csharp
var connection = db.Database.GetDbConnection();
var transaction = db.Database.CurrentTransaction?.GetDbTransaction();
var rows = await connection.QueryAsync<Row>(sql, parameters, transaction);
```

## ADO.NET directly

For streaming very large result sets, or when you need exact control:

```csharp
await using var command = connection.CreateCommand();
command.CommandText = "SELECT id, reference FROM orders WHERE tenant_id = $1";
command.Parameters.Add(new NpgsqlParameter { Value = tenantId });

await using var reader = await command.ExecuteReaderAsync(ct);
while (await reader.ReadAsync(ct))
    yield return new OrderRow(reader.GetGuid(0), reader.GetString(1));
```

## Non-negotiable rules

- **Every** value from outside the process is a parameter. No exceptions, no "it's just an internal admin page".
- Table and column names cannot be parameterised — validate them against an allowlist if they must be dynamic.
- Keep SQL in one place per feature (a repository, a `.sql` resource), not scattered through handlers.
- Set a command timeout; a query with no timeout is an outage waiting for a bad plan.

## Further reading

- [Raw SQL queries in EF Core](https://learn.microsoft.com/ef/core/querying/sql-queries)
- [Dapper](https://github.com/DapperLib/Dapper)
