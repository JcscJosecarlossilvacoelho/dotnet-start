---
name: dotnet-testing
description: Write, review, or repair .NET tests. Use for xUnit and NUnit test design, naming, assertions, fakes versus mocks, integration tests with WebApplicationFactory and Testcontainers, flaky tests, and coverage decisions.
---

# Testing .NET

Read the existing test project before adding anything: its framework (`xunit`, `nunit`, `MSTest`, `TUnit`), its assertion library, and its fixture conventions. Use what is there. Introducing a second test framework or assertion style into a repository is a cost, not an improvement.

```bash
rg --files -g '*.Tests.csproj' -g '*/Tests/*.csproj'
rg -n 'PackageReference' -g '*.Tests.csproj' -g '*/Tests/*.csproj'
```

## Decide what kind of test this is

| Behavior under test | Test |
| --- | --- |
| A decision, calculation, or state machine | Unit test, no test doubles at all if possible |
| A rule that spans a few collaborating types | Unit test with real collaborators, fake only I/O |
| An HTTP contract — status, shape, auth | Integration test via `WebApplicationFactory` |
| A query, mapping, migration, or constraint | Integration test against the real provider (Testcontainers) |
| A whole user journey through the browser | One or two smoke tests, no more |

Most value sits in the middle two rows. Do not mock what you own just to reach 100% line coverage; test through the public seam instead.

## Write the test so a failure explains itself

```csharp
[Fact]
public async Task Checkout_returns_conflict_when_stock_is_exhausted()
{
    var client = _factory.CreateClient();
    await Seed(new Product("SKU-1", stock: 0));

    var response = await client.PostAsJsonAsync("/checkout", new { sku = "SKU-1", quantity = 1 });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal("Out of stock", problem!.Title);
}
```

- Name the test for the behavior and the condition, not the method: `Method_state_expectation`. The name is what a failing CI run shows first.
- One arrange/act/assert per test, separated by blank lines. One logical assertion — several `Assert` calls checking one outcome is fine.
- Use `[Theory]` + `[InlineData]`/`[MemberData]` for the same behavior over different inputs; do not loop inside a test.
- Assert on observable outcomes: the returned value, the response, the persisted row. Never assert that a mock was called when you can assert on the effect.
- Build data with a small builder or factory method so each test states only what makes it different.

## Test doubles

- Prefer a hand-written fake (an in-memory `IOrderStore`) over a mocking framework when the interface is yours and small — it is reusable, refactor-safe, and readable.
- Use a mocking library (NSubstitute, Moq) for third-party interfaces and for verifying that something was *not* called.
- Never mock `DbContext`, `HttpClient`, or `ILogger<T>`. Use the real `DbContext` against a container, `HttpMessageHandler` stubs (or `Microsoft.Extensions.Http.Testing`), and `NullLogger<T>` / a capturing `FakeLogger`.
- `TimeProvider` and `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) replace `DateTime.UtcNow` and `Task.Delay`. Inject `TimeProvider` in production code so time is testable at all.

## Integration tests

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

    public Task InitializeAsync() => _db.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());

    public new async Task DisposeAsync() { await _db.DisposeAsync(); await base.DisposeAsync(); }
}
```

- Boot the app the way production boots it, then override only the external edges (database, clock, outbound HTTP). Every `services.Remove(...)` in a test fixture is a gap between what you tested and what you ship.
- Reuse one container per test class or collection; starting one per test is the usual cause of a slow suite.
- Isolate tests with a transaction rollback or a per-test schema, not by ordering them. Tests must pass in any order and in parallel.
- Use EF's in-memory provider only for code with no relational semantics. It does not enforce constraints, and it will let broken queries pass.

## Fix a flaky test, do not retry it

Flakiness is almost always one of: real wall-clock waits (`Task.Delay` instead of `FakeTimeProvider`), shared mutable state between tests, dependence on order or on `DateTime.Now`, an unawaited task, or culture/timezone assumptions. Find which, and fix that. Retry attributes hide a race that will reappear in production.

## Complete the change

```bash
dotnet test                                    # whole suite, not just the new test
dotnet test --filter "FullyQualifiedName~Checkout"
dotnet test --collect:"XPlat Code Coverage"    # when coverage is the question
```

Confirm the new test fails without the production change — a test that passes against unmodified code tests nothing. Report the failing-then-passing observation rather than only the final green run.
