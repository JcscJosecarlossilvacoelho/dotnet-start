---
title: Domain modelling
description: Making invalid states unrepresentable — entities, value objects, and where rules live.
order: 30
---

The goal is narrow: a type whose values are always valid needs no defensive check anywhere else in the program.

## Value objects

```csharp
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (currency.Length != 3) throw new ArgumentException("Use an ISO 4217 code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money operator +(Money left, Money right) =>
        left.Currency == right.Currency
            ? new Money(left.Amount + right.Amount, left.Currency)
            : throw new InvalidOperationException("Cannot add different currencies.");
}
```

`decimal total` invites bugs; `Money` prevents a whole class of them, including adding euros to dollars, at zero runtime cost.

## Entities own their invariants

```csharp
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }                                   // for EF Core

    public static Order Draft(CustomerId customer) => new() { Id = OrderId.New(), CustomerId = customer };

    public OrderId Id { get; private init; }
    public CustomerId CustomerId { get; private init; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderLine> Lines => _lines;

    public void AddLine(Sku sku, int quantity, Money unitPrice)
    {
        if (Status is not OrderStatus.Draft)
            throw new InvalidOperationException("Only a draft order can be changed.");

        _lines.Add(new OrderLine(sku, quantity, unitPrice));
    }

    public void Place(DateTimeOffset when)
    {
        if (_lines.Count == 0) throw new InvalidOperationException("An order needs at least one line.");

        Status = OrderStatus.Placed;
        Placed = when;
    }
}
```

Public setters everywhere mean any code, anywhere, can produce an invalid order. Private setters plus intention-revealing methods mean the rules live in one file you can read.

## Where rules belong

| Rule | Home |
| --- | --- |
| Always true of the entity | The entity |
| Involves several entities | A domain service |
| Depends on the request shape | [Request validation](/docs/web/validation) |
| Depends on data elsewhere | The application handler, before calling the domain |

## Persistence should follow the model

EF Core maps private setters, backing fields, owned types, and value conversions — so you can model properly and still map cleanly:

```csharp
builder.Metadata.FindNavigation(nameof(Order.Lines))!.SetPropertyAccessMode(PropertyAccessMode.Field);
builder.Property(o => o.Id).HasConversion(id => id.Value, value => new OrderId(value));
```

See [Modelling entities](/docs/data/modeling).

## How far to take it

Full DDD — aggregates, repositories per aggregate, domain events, bounded contexts — is worth it in a complex core domain with real rules. For CRUD over a form, it is expensive ceremony. Model richly where the rules are; keep the rest simple and boring.

## Further reading

- [DDD-oriented microservice design](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
