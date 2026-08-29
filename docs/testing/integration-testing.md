---
title: Integration testing
description: Running the real application in memory with WebApplicationFactory, and testing against real dependencies.
order: 20
---

Integration tests exercise routing, model binding, filters, authorization, dependency injection, and serialization — the parts most likely to break and least likely to be covered by unit tests.

## WebApplicationFactory

```bash
dotnet add MyApp.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
```

```csharp
public class OrderApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Returns_404_for_an_unknown_order()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

The application starts in memory with its real pipeline. No sockets, no ports, no flakiness from a background process that did not finish starting.

Make `Program` visible to the test project:

```csharp
public partial class Program;    // at the bottom of Program.cs
```

## Replacing dependencies

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        });
    }
}
```

Replace what you cannot call (a payment provider, an email sender). Keep everything you can run — especially the database.

## A real database with Testcontainers

```csharp
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).Options);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

The in-memory EF provider is not a database: it has no transactions, no constraints, no SQL translation, and no concurrency. A container gives you the real engine and catches the bugs that matter. Reuse one container per test collection; reset state between tests by truncating or by wrapping each test in a rolled-back transaction.

## Testing authorization

```csharp
var client = factory.CreateClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForUser("alice", "orders:read"));
```

Or register a test authentication handler that reads the identity from a header. Then assert all three outcomes: 401 anonymous, 403 wrong user, 200 right user. Authorization is exactly the logic you cannot afford to regress.

## Keeping the suite fast and honest

- Parallelise across collections; keep shared containers per collection, not per test.
- No `Thread.Sleep`. Poll with a timeout, or expose a deterministic hook.
- Every test creates its own data with unique keys — shared fixtures that mutate are where flakiness comes from.
- Run the suite in CI on every pull request. A test that only runs locally protects nobody.

## Further reading

- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
