---
title: Dates, times, and globalization
description: Choosing the right date type, handling time zones, and formatting for machines versus humans.
order: 40
---

## Choosing a type

| Type | Represents | Use for |
| --- | --- | --- |
| `DateTimeOffset` | An instant, with UTC offset | Timestamps: created, placed, logged |
| `DateOnly` | A calendar date | Birthdays, invoice dates, holidays |
| `TimeOnly` | A wall-clock time | Opening hours, alarms |
| `TimeSpan` | A duration | Timeouts, elapsed time |
| `DateTime` | Ambiguous — depends on `Kind` | Legacy code and interop only |
| `TimeZoneInfo` | A zone with its rules | Converting for display or scheduling |

Default to `DateTimeOffset` for instants and `DateOnly` for dates. `DateTime` carries a `Kind` that is easy to lose across serialization, which is how a timestamp silently shifts by an hour.

## Storing and converting

Store instants in UTC. Convert to a zone only for display or for zone-dependent business rules:

```csharp
var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");   // IANA ids work on all platforms
var local = TimeZoneInfo.ConvertTime(order.PlacedAt, zone);
```

"Every weekday at 09:00 Lisbon time" is not a fixed UTC time — daylight saving moves it. Schedule in the zone, convert at each occurrence.

## Testable time

```csharp
public sealed class OrderService(TimeProvider time)
{
    public Order Place(Cart cart) => Order.From(cart, placedAt: time.GetUtcNow());
}
```

Inject `TimeProvider`; use `FakeTimeProvider` in tests. Calling `DateTimeOffset.UtcNow` inside a service makes time-dependent behaviour untestable and flaky at boundaries.

## Formatting

```csharp
value.ToString("O", CultureInfo.InvariantCulture);        // round-trip, for machines
amount.ToString("C", new CultureInfo("pt-PT"));           // currency, for humans
decimal.Parse(input, CultureInfo.InvariantCulture);       // parsing machine input
```

**Always pass a culture.** The default culture is the machine's, which means the same code produces `1,5` on one server and `1.5` on another — a class of bug that only appears in production.

Use `"O"` (ISO 8601 / RFC 3339) for anything that crosses a wire or a file boundary.

## String comparison

```csharp
name.Equals(other, StringComparison.Ordinal);                // identifiers, keys, tokens
name.Equals(other, StringComparison.OrdinalIgnoreCase);      // case-insensitive identifiers
name.StartsWith(prefix, StringComparison.CurrentCulture);    // user-facing sorting
```

Ordinal for anything the machine owns; culture-aware only for text the user reads. Culture-sensitive comparison of identifiers is how the Turkish dotless-i bug happens.

## InvariantGlobalization

```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

Drops ICU: smaller containers, faster startup, but only the invariant culture, and culture-specific comparisons and formats change behaviour. Fine for an internal API; not for anything rendering localised output.

## Further reading

- [Date, time, and time zone](https://learn.microsoft.com/dotnet/standard/datetime/)
- [Globalization and localization](https://learn.microsoft.com/dotnet/core/extensions/globalization-and-localization)
