---
title: Unit testing
description: Frameworks, structure, and what a unit test should actually assert.
order: 10
---

## Choosing a framework

| Framework | Notes |
| --- | --- |
| xUnit | The de-facto default in .NET; a new instance per test, no `[SetUp]` |
| NUnit | Rich assertions and parameterisation; familiar from older codebases |
| MSTest | Ships with Visual Studio; fine, rarely the reason to choose |

```bash
dotnet new xunit -n MyApp.UnitTests
dotnet add MyApp.UnitTests reference src/MyApp.Domain
dotnet test
```

## A test that reads well

```csharp
public class OrderTotalTests
{
    [Fact]
    public void Applies_free_shipping_above_the_threshold()
    {
        var order = new Order([new OrderLine("sku-1", Quantity: 2, UnitPrice: 30m)]);

        var total = order.CalculateTotal(shippingCost: 5m, freeShippingFrom: 50m);

        Assert.Equal(60m, total);
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(49.99, 5, 54.99)]
    [InlineData(50, 5, 50)]
    public void Charges_shipping_below_the_threshold(decimal subtotal, decimal shipping, decimal expected)
        => Assert.Equal(expected, Order.Total(subtotal, shipping, freeShippingFrom: 50m));
}
```

- The name states the behaviour, not the method under test.
- Arrange, act, assert — with blank lines, not comments.
- One behaviour per test. A test that asserts five things fails without telling you which rule broke.

## What to unit test

Test the code where a mistake is expensive and the logic is real: pricing, validation, state transitions, parsing, permission rules. Do not write tests that restate the implementation (`Assert.Equal(2, 1 + 1)` in a fancy hat), and do not test the framework.

Anything that touches a database, the network, or the clock is an [integration test](/docs/testing/integration-testing) — writing a maze of mocks to keep it "unit" produces a test that passes while production fails.

## Test doubles

```csharp
var repository = Substitute.For<IOrderRepository>();      // NSubstitute
repository.FindAsync(id, Arg.Any<CancellationToken>()).Returns(order);
```

Mock what you own and what represents an external boundary. Two rules keep mocks useful:

1. Do not mock types you do not own — wrap them in your own interface first.
2. Assert on behaviour, not on interactions, unless the interaction *is* the behaviour (a message was published, an email was sent).

Hand-written fakes (an in-memory repository) often age better than mock setups, because they break when the interface changes rather than silently returning `null`.

## Controlling time and randomness

Inject `TimeProvider` and use `FakeTimeProvider` in tests. Inject a `Random` seeded per test. Code that calls `DateTime.UtcNow` or `Guid.NewGuid()` directly is code you cannot test deterministically — and it will fail at midnight in a different timezone.

## Running them

```bash
dotnet test                                        # everything
dotnet test --filter "FullyQualifiedName~Order"    # by name
dotnet test --collect:"XPlat Code Coverage"        # coverage data
```

Coverage is a smoke detector, not a goal. 100% coverage with no assertions is worse than 60% with sharp ones.

## Further reading

- [Unit testing in .NET](https://learn.microsoft.com/dotnet/core/testing/)
