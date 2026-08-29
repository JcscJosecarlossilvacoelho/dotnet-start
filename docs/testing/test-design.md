---
title: Designing a test suite
description: What to test at which level, how to keep tests readable, and how to stop a suite from rotting.
order: 30
---

## The shape

Think in terms of confidence per second of runtime:

| Level | Count | Runtime | Answers |
| --- | --- | --- | --- |
| Unit | Many | Milliseconds | Is this rule correct? |
| Integration | Fewer | Seconds | Do the parts fit together? |
| End-to-end | Few | Minutes | Does the critical journey work? |

The exact ratio matters less than the principle: push a test as low as it can go while still failing for a real reason.

## Test behaviour, not structure

A test coupled to private methods and internal call sequences breaks on every refactor and never catches a bug. Test through the public surface — a domain method, an HTTP endpoint, a message handler — so the test survives the rewrite and still means something after it.

## Naming and readability

```csharp
[Fact]
public async Task Rejects_a_second_payment_for_the_same_idempotency_key() { ... }
```

Read the failure output of your test suite as a specification. If the names do not describe the system's behaviour, they are wasted.

Build small factories for test data rather than repeating twelve-line object initialisers:

```csharp
internal static class Orders
{
    public static Order Draft(Action<Order>? customise = null) { ... }
}
```

## Deterministic by construction

Flakiness destroys the value of a suite faster than any missing coverage. Eliminate its sources:

- Time — inject `TimeProvider`, use `FakeTimeProvider`.
- Randomness and ids — seed them, or assert on shape not value.
- Ordering — never let one test depend on another having run.
- Concurrency — no fixed sleeps; wait for a condition with a timeout.
- Shared state — unique keys and per-test data.

Quarantine a flaky test immediately and fix it or delete it. A suite people re-run "because it does that sometimes" is no longer a signal.

## Snapshot and contract tests

Snapshot testing (Verify) is excellent for serializers, generated SQL, and API payloads: the diff *is* the review. Contract-check your [OpenAPI document](/docs/web/openapi) in CI so a breaking change to a client is caught before the client's team is.

## Mutation testing

Coverage says a line ran; mutation testing (Stryker.NET) says a test would fail if the line were wrong. Running it occasionally on your most critical module is the cheapest way to find out whether your assertions actually assert.

## Tests and coding agents

A suite is what makes agent-written changes safe to accept: it turns "looks right" into "provably still works". Two habits pay for themselves — keep tests runnable with a single `dotnet test`, and make failure output say what broke rather than `Assert.True` was false.

## Further reading

- [Unit testing best practices](https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices)
