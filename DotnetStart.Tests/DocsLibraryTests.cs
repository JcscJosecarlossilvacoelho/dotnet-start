using DotnetStart.Docs;

namespace DotnetStart.Tests;

public sealed class DocsLibraryTests
{
    [Fact]
    public void Loads_pages_from_section_folders_and_ignores_root_and_section_files()
    {
        using var docs = new TempDocs();
        docs.Write("README.md", "# ignored");
        docs.Write("start/_section.md", """
            ---
            title: Getting started
            description: From zero to running.
            order: 10
            ---
            """);
        docs.Write("start/getting-started.md", TempDocs.Page("First app", "Create a project.", order: 20));
        docs.Write("start/what-is-dotnet.md", TempDocs.Page("What is .NET", "A platform.", order: 10));
        docs.Write(".hidden/secret.md", TempDocs.Page("Secret", "Nope."));
        docs.Write("_drafts/wip.md", TempDocs.Page("WIP", "Nope."));

        var library = docs.Library();

        Assert.Equal(["start"], library.Sections.Select(s => s.Id));
        Assert.Equal("Getting started", library.Sections[0].Title);
        Assert.Equal(["start/what-is-dotnet", "start/getting-started"], library.AllDocuments.Select(d => d.Slug));
        Assert.Null(library.Find("README"));
        Assert.Null(library.Find("start/_section"));
        Assert.Null(library.Find(".hidden/secret"));
    }

    [Fact]
    public void Orders_sections_then_pages_and_falls_back_to_a_humanised_filename()
    {
        using var docs = new TempDocs();
        docs.Write("web/_section.md", """
            ---
            title: Web
            order: 20
            ---
            """);
        docs.Write("start/_section.md", """
            ---
            title: Start
            order: 10
            ---
            """);
        docs.Write("web/routing.md", TempDocs.Page("Routing", "URLs.", order: 10));
        docs.Write("start/untitled.md", """
            ---
            description: No title on purpose.
            order: 10
            ---

            Body.
            """);

        var library = docs.Library();

        Assert.Equal(["start/untitled", "web/routing"], library.AllDocuments.Select(d => d.Slug));
        Assert.Equal("Untitled", library.Find("start/untitled")!.Title);
        Assert.Equal(1, library.Find("start/untitled")!.Number);
        Assert.Equal(2, library.Find("web/routing")!.Number);
        Assert.Contains("<p>Body.</p>", library.Find("start/untitled")!.Html);
    }

    [Fact]
    public void Find_neighbours_and_search_rank_title_hits_above_body_hits()
    {
        using var docs = new TempDocs();
        docs.Write("start/_section.md", """
            ---
            title: Start
            order: 10
            ---
            """);
        docs.Write("start/alpha.md", TempDocs.Page("Alpha", "Nothing of interest.", order: 10));
        docs.Write("start/beta.md", TempDocs.Page("A guide to widgets", "Something else.", order: 20));
        docs.Write("start/gamma.md", TempDocs.Page("Gamma", "Widgets appear only here.", order: 30));

        var library = docs.Library();
        var beta = library.Find("start/beta")!;
        var (previous, next) = library.Neighbours(beta);

        Assert.Equal("start/alpha", previous!.Slug);
        Assert.Equal("start/gamma", next!.Slug);
        Assert.Null(library.Neighbours(library.Find("start/alpha")!).Previous);
        Assert.Null(library.Neighbours(library.Find("start/gamma")!).Next);

        var hits = library.Search("widgets");
        Assert.Equal(["start/beta", "start/gamma"], hits.Select(d => d.Slug));
        Assert.Empty(library.Search("no-such-token"));
        Assert.Equal(library.AllDocuments.Take(2).Select(d => d.Slug), library.Search("  ", take: 2).Select(d => d.Slug));
        Assert.Equal(["start/gamma"], library.Search("widgets appear").Select(d => d.Slug));
    }

    [Fact]
    public void Reloads_when_a_file_is_deleted_even_if_it_was_the_newest()
    {
        using var docs = new TempDocs();
        docs.Write("start/_section.md", """
            ---
            title: Start
            order: 10
            ---
            """);
        docs.Write("start/kept.md", TempDocs.Page("Kept", "Stay.", order: 10), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        docs.Write("start/removed.md", TempDocs.Page("Removed", "Go.", order: 20), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var library = docs.Library();
        Assert.Equal(2, library.AllDocuments.Count);

        docs.Delete("start/removed.md");

        Assert.Single(library.AllDocuments);
        Assert.Null(library.Find("start/removed"));
        Assert.NotNull(library.Find("start/kept"));
    }

    [Fact]
    public void Reloads_when_a_file_is_added_with_an_older_timestamp()
    {
        using var docs = new TempDocs();
        docs.Write("start/_section.md", """
            ---
            title: Start
            order: 10
            ---
            """);
        docs.Write("start/new.md", TempDocs.Page("New", "Fresh.", order: 10), new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var library = docs.Library();
        Assert.Single(library.AllDocuments);

        docs.Write("start/old.md", TempDocs.Page("Old", "Stale stamp.", order: 20), new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, library.AllDocuments.Count);
        Assert.NotNull(library.Find("start/old"));
    }

    [Fact]
    public void Production_scan_interval_does_not_pick_up_a_new_file()
    {
        using var docs = new TempDocs();
        docs.Write("start/_section.md", """
            ---
            title: Start
            order: 10
            ---
            """);
        docs.Write("start/only.md", TempDocs.Page("Only", "One page."));

        var library = docs.Library(Timeout.InfiniteTimeSpan);
        library.Warm();
        docs.Write("start/later.md", TempDocs.Page("Later", "Should stay invisible."));

        Assert.Single(library.AllDocuments);
        Assert.Null(library.Find("start/later"));
    }

    [Fact]
    public void Missing_folder_is_empty_and_recovers_when_the_folder_appears()
    {
        var missing = Path.Combine(Path.GetTempPath(), "dotnet-start-missing-" + Guid.NewGuid().ToString("n"));
        var library = new DocsLibrary(missing, TimeSpan.Zero);

        Assert.Empty(library.AllDocuments);
        Assert.Null(library.Find("start/anything"));

        Directory.CreateDirectory(Path.Combine(missing, "start"));
        File.WriteAllText(
            Path.Combine(missing, "start", "hello.md"),
            TempDocs.Page("Hello", "Recovered."));

        try
        {
            Assert.NotNull(library.Find("start/hello"));
        }
        finally
        {
            Directory.Delete(missing, recursive: true);
        }
    }

    [Fact]
    public void Loads_the_repository_docs()
    {
        var library = new DocsLibrary(Path.Combine(RepoRoot.Find(), "docs"), Timeout.InfiniteTimeSpan);

        Assert.NotNull(library.Find("start/what-is-dotnet"));
        Assert.NotEmpty(library.Sections);
        Assert.True(library.AllDocuments.Count > 20);
        Assert.All(library.AllDocuments, doc =>
        {
            Assert.False(string.IsNullOrWhiteSpace(doc.Title));
            Assert.False(string.IsNullOrWhiteSpace(doc.Html));
            Assert.StartsWith("/docs/", doc.Href);
        });
    }
}
