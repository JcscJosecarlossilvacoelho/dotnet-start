namespace DotnetStart.Hosting;

/// <summary>
/// Vercel serves its analytics scripts from <c>/_vercel/*</c> on its own edge, so the
/// tags are only meaningful in the crawled static build. Everywhere else — a container,
/// <c>dotnet run</c>, Fly, Render — those paths are two guaranteed 404s, so the pages
/// leave them out unless <c>VERCEL_ANALYTICS_ENABLED</c> says otherwise.
/// <see cref="ForwardedHeadersSetup"/> uses the same opt-in shape.
/// </summary>
public static class VercelAnalytics
{
    public const string EnabledVariable = "VERCEL_ANALYTICS_ENABLED";

    /// <summary>Page views, per route.</summary>
    public const string WebAnalyticsScript = "/_vercel/insights/script.js";

    /// <summary>Core Web Vitals, measured on real visits.</summary>
    public const string SpeedInsightsScript = "/_vercel/speed-insights/script.js";

    public static bool IsEnabled()
        => ForwardedHeadersSetup.IsTruthy(Environment.GetEnvironmentVariable(EnabledVariable));
}
