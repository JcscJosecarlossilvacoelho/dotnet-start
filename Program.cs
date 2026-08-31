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

// Every component renders statically. Search, the feedback prompt and the copy
// buttons are plain JavaScript over data attributes, so there is no circuit to
// keep alive and the whole site can be crawled to flat HTML for a static host.
builder.Services.AddRazorComponents();

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

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Font files are referenced by name from inside app.css, so they cannot
        // carry a build-time fingerprint the way the stylesheet itself does.
        // They are versioned by their filename and never edited in place, so
        // cache them hard rather than revalidating one per page view.
        if (context.Context.Request.Path.StartsWithSegments("/fonts"))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
});

app.UseRouting();

app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Text("ok"));

// The client-side search index. Written to a file by the prerender script and
// served from the CDN there; served live here.
app.MapGet("/search-index.json", (DocsLibrary library) => Results.Json(
    library.AllDocuments.Select(doc => new
    {
        t = doc.Title,
        d = doc.Description,
        s = doc.Slug,
        h = doc.Href,
    })));

// Every URL the site publishes: what the prerender crawler walks, and a plain
// sitemap for crawlers that want one.
app.MapGet("/sitemap.txt", (DocsLibrary library) => Results.Text(
    string.Join('\n', new[] { "/", "/docs", "/skills" }
        .Concat(library.AllDocuments.Select(doc => doc.Href)))));

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Before the first request, not during it.
app.Services.GetRequiredService<DocsLibrary>().Warm();

app.Run();
