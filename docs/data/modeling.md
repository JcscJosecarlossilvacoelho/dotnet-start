---
title: Modelling entities
description: Configuring the model deliberately — keys, relationships, owned types, value conversions, and indexes.
order: 20
---

Conventions get you started; explicit configuration keeps the database honest. Put configuration in one class per entity, not in `OnModelCreating`.

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Reference).HasMaxLength(64).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => o.Reference).IsUnique();
        builder.HasIndex(o => new { o.CustomerId, o.Placed });

        builder.HasMany(o => o.Lines)
               .WithOne()
               .HasForeignKey(l => l.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.Line1).HasColumnName("shipping_line1").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("shipping_city").HasMaxLength(100);
        });
    }
}
```

## Relationships

| Shape | Configuration |
| --- | --- |
| One-to-many | `HasMany(...).WithOne(...).HasForeignKey(...)` |
| One-to-one | `HasOne(...).WithOne(...).HasForeignKey<TDependent>(...)` |
| Many-to-many | `HasMany(...).WithMany(...)` — a join table is created automatically |
| Self-referencing | `HasOne(x => x.Parent).WithMany(x => x.Children)` |

Choose `DeleteBehavior` deliberately: `Cascade` for true composition (order lines), `Restrict` when deleting the principal should fail, `SetNull` for optional links.

## Value conversions

```csharp
builder.Property(o => o.Currency).HasConversion(
    v => v.Code,
    v => Currency.FromCode(v));

builder.Property(o => o.Tags).HasConversion(
    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
    v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default)!);
```

Converted properties cannot generally be translated into SQL predicates in a useful way — a filter on a JSON blob will scan. Convert for storage convenience, not for querying.

## Strongly typed ids

```csharp
public readonly record struct OrderId(Guid Value);

builder.Property(o => o.Id).HasConversion(id => id.Value, value => new OrderId(value));
```

They eliminate a whole class of bug: passing a `CustomerId` where an `OrderId` was expected no longer compiles.

## Owned types vs separate entities

Own a type when it has no identity of its own and no lifetime apart from its parent (an address, a money amount, a period). Make it an entity when it can be queried or referenced independently.

## Indexes are part of the model

Every foreign key you filter or join on, every column you sort by, and every unique business key deserves an index — declared here so it travels with the [migrations](/docs/data/migrations) rather than living in someone's shell history.

## Further reading

- [Creating and configuring a model](https://learn.microsoft.com/ef/core/modeling/)
