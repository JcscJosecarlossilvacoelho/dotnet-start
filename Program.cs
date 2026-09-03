using DotnetStart.Hosting;
using DotnetStart.Components;
using DotnetStart.Content;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Render, Cloud Run, Heroku and friends hand the port to bind on in $PORT and
// terminate TLS at their own edge, forwarding plain HTTP inwards. $PORT only
// means "bind here and do not redirect to an HTTPS port this process does not
// have". Trusting X-Forwarded-* is a separate, explicit decision: see
// ForwardedHeadersSetup, render.yaml, and fly.toml.
var port = Environment.GetEnvironmentVariable("PORT");
var bindToPlatformPort = !string.IsNullOrWhiteSpace(port);
var trustForwardedHeaders = ForwardedHeadersSetup.IsEnabled();

if (bindToPlatformPort)
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

if (trustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(ForwardedHeadersSetup.Apply);
}

builder.Services.AddSingleton<DocsLibrary>();
builder.Services.AddSingleton<SkillsLibrary>();

// Every component renders statically. Search, the feedback prompt and the copy
// buttons are plain JavaScript over data attributes, so there is no circuit to
// keep alive and the whole site can be crawled to flat HTML for a static host.
builder.Services.AddRazorComponents();

var app = builder.Build();

if (trustForwardedHeaders)
{
    // Must run before anything reads the scheme — redirects and any WebSocket
    // upgrade need the original https so they do not advertise http/ws to the
    // public edge.
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

if (!bindToPlatformPort)
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
app.MapGet("/sitemap.txt", (DocsLibrary library, SkillsLibrary skills) => Results.Text(
    string.Join('\n', new[] { "/", "/docs", "/skills" }
        .Concat(library.AllDocuments.Select(doc => doc.Href))
        .Concat(skills.All.Select(skill => skill.Href))) + '\n'));

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Before the first request, not during it.
app.Services.GetRequiredService<DocsLibrary>().Warm();
// Constructing the singleton is the load: SkillsLibrary reads in its constructor.
app.Services.GetRequiredService<SkillsLibrary>();

app.Run();

public partial class Program;
