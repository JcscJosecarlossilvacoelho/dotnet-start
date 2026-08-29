---
title: Migrations
description: Evolving a schema safely — generating, reviewing, and applying migrations without downtime.
order: 30
---

A migration is generated C# describing the difference between the model and the last known schema. It is code: review it like code.

## The loop

```bash
dotnet tool install dotnet-ef            # local tool, versioned with the repo
dotnet ef migrations add AddOrderStatus
dotnet ef migrations script              # read the SQL before it touches anything
dotnet ef database update                # development only
```

Always read the generated `Up`. EF cannot tell a rename from a drop-and-add: an unreviewed migration is how columns of production data disappear.

## Applying in production

Do **not** call `Database.Migrate()` at application startup in a multi-instance deployment — several instances will race, and a failed migration takes the whole rollout with it. Instead:

```bash
dotnet ef migrations bundle --self-contained -r linux-x64 -o ./migrate
./migrate --connection "$CONNECTION_STRING"
```

A migration bundle is a single executable you run as a deployment step, with the database credentials the application itself does not need.

Generate idempotent SQL when a DBA applies it:

```bash
dotnet ef migrations script --idempotent --output migrate.sql
```

## Zero-downtime changes

Old and new code run simultaneously during a rollout, so every schema change must be compatible with both. Use the expand/contract pattern:

1. **Expand** — add the new nullable column or table. Deploy.
2. **Backfill** — copy data in batches, outside the migration if it is large.
3. **Migrate code** — write to both, read from the new. Deploy.
4. **Contract** — drop the old column. Deploy.

Changes that are safe in one step: adding a nullable column, adding a table, adding an index concurrently. Changes that are never safe in one step: renaming, changing a type, adding a NOT NULL column without a default, dropping anything still referenced.

## Long-running index builds

```csharp
migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_orders_placed ON orders (placed);",
    suppressTransaction: true);
```

Postgres cannot build a concurrent index inside a transaction; other engines have equivalents. Know your database's locking behaviour before shipping a migration that takes a table lock.

## Hygiene

- One migration per pull request; a descriptive name (`AddOrderStatus`, not `Update3`).
- Never edit a migration that has been applied anywhere — add a new one.
- Resolve merge conflicts in the model snapshot by regenerating, not by hand-merging.
- Test the migration against a copy of production-sized data before it is a production incident.

## Further reading

- [Migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
