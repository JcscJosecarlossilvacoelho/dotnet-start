using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DotnetStart.Tests;

public sealed class SiteWebTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public SiteWebTests(SiteFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/docs")]
    [InlineData("/skills")]
    [InlineData("/docs/start/what-is-dotnet")]
    [InlineData("/healthz")]
    [InlineData("/search-index.json")]
    [InlineData("/sitemap.txt")]
    public async Task Published_routes_succeed(string path)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Missing_guide_is_an_http_404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/docs/this-guide-does-not-exist");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("This guide is not here yet", body);
    }

    [Fact]
    public async Task Unknown_route_is_an_http_404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/not-a-real-route");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("This page has not", body);
    }
}

public sealed class SiteFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(RepoRoot.Find());
        builder.UseEnvironment("Development");
    }
}
