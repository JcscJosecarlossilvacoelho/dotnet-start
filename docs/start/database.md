---
title: Database
description: EF Core with a real model, a migration, and queries you can read.
order: 20
---

# Database

Use **Entity Framework Core**. It is the default for new .NET apps: typed models, migrations, and LINQ that compiles.

## Add the provider

Pick one provider and stick with it. PostgreSQL is the usual production choice; SQLite is fine on a laptop.

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

## Define a model and a context

Keep the entity as the source of truth. Map it once, then query it.

```csharp
public sealed class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool Done { get; set; }
}

public sealed class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    public DbSet<Todo> Todos => Set<Todo>();
}
```

Register it in `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("App")));
```

## Migrate for real

Do not call `EnsureCreated` in an app you intend to keep. Generate a migration and apply it.

```bash
dotnet ef migrations add Initial
dotnet ef database update
```

## Expose it through the API

```csharp
app.MapGet("/todos", async (AppDb db) =>
    await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync());
```

`AsNoTracking` on reads. Track only when you are about to save.

## Build it with an agent

> Add EF Core with PostgreSQL to this ASP.NET Core app. Create a Todo entity, an AppDb context, a connection string, an Initial migration, and a GET /todos endpoint that returns items AsNoTracking. Explain why we migrate instead of calling EnsureCreated.

## Next

Protect those endpoints with authentication instead of leaving them open.
