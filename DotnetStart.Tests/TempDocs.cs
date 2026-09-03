using DotnetStart.Docs;

namespace DotnetStart.Tests;

internal sealed class TempDocs : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "dotnet-start-" + Guid.NewGuid().ToString("n"));

    public TempDocs() => Directory.CreateDirectory(Root);

    public string Write(string relativePath, string content, DateTime? lastWriteUtc = null)
    {
        var path = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
        if (lastWriteUtc is { } stamp) File.SetLastWriteTimeUtc(path, stamp);
        return path;
    }

    public void Delete(string relativePath) => File.Delete(Path.Combine(Root, relativePath));

    public DocsLibrary Library(TimeSpan? rescanAfter = null) => new(Root, rescanAfter ?? TimeSpan.Zero);

    public static string Page(
        string title,
        string body,
        string? description = null,
        int order = 10)
    {
        return $"""
            ---
            title: {title}
            description: {description ?? title}
            order: {order}
            ---

            {body}
            """;
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
