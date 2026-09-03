namespace DotnetStart.Hosting;

/// <summary>
/// Blazor's static renderer discards the HTML buffer if the status is already 404
/// when it flushes. Register the status on <see cref="HttpResponse.OnStarting"/>
/// so the missing-guide page still reaches the client.
/// </summary>
internal static class NotFoundStatus
{
    public static void Mark(HttpContext? http)
    {
        if (http is null || http.Response.HasStarted) return;

        http.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state!;
            if (response.StatusCode == StatusCodes.Status200OK)
                response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }, http.Response);
    }
}
