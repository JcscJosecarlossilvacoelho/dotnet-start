using DotnetStart.Components;
using DotnetStart.Docs;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Render, Cloud Run, Heroku and friends hand the port to bind on in $PORT and
// terminate TLS at their own edge, forwarding plain HTTP inwards. Treat the
// presence of $PORT as "there is a proxy in front of me": bind where the
// platform is looking, and trust its forwarded headers instead of trying to
// redirect to an HTTPS port this process does not have.
var port = Environment.GetEnvironmentVariable("PORT");
var behindProxy = !string.IsNullOrWhiteSpace(port);

if (behindProxy)
{
    builder.WebHost.UseUrls($"http://+:{port}");

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // The proxy is the platform's own load balancer on an address we cannot
        // predict, so the default allow-list would drop its headers.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddSingleton<DocsLibrary>();
builder.Services.AddScoped<DocsSearchState>();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (behindProxy)
{
    // Must run before anything reads the scheme — the SignalR circuit needs to
    // know it arrived over HTTPS so it negotiates wss:// rather than ws://.
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

if (!behindProxy)
{
    app.UseHttpsRedirection();
}

// Razor component endpoints answer GET, so a HEAD request — a platform's port
// scan, most uptime monitors — gets a 405. Serve it as a GET with the body
// thrown away, which is what a HEAD response is. This has to run before the
// endpoint is chosen, hence the explicit UseRouting below: without it,
// WebApplication inserts routing at the very top of the pipeline and the method
// has already been matched by the time this rewrite happens.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsHead(context.Request.Method))
    {
        context.Request.Method = HttpMethods.Get;
        context.Response.Body = Stream.Null;
    }

    await next();
});

app.UseRouting();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Text("ok"));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
