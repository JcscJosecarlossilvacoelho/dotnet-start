using DotnetStart.Content;

namespace DotnetStart.Tests;

public sealed class MarkdownTests
{
    [Fact]
    public void StripLeadingH1_drops_only_the_first_heading()
    {
        Assert.Equal("Body.\n", DocsLibrary.StripLeadingH1("# Title\nBody.\n"));
        Assert.Equal("## Still here\n", DocsLibrary.StripLeadingH1("## Still here\n"));
        Assert.Equal(string.Empty, DocsLibrary.StripLeadingH1("# Title only"));
        Assert.Equal("plain", DocsLibrary.StripLeadingH1("plain"));
    }

    [Fact]
    public void Headings_are_level_two_with_github_ids()
    {
        var headings = DocsLibrary.Headings("""
            # Ignored
            ## Routing
            ### Nested
            ## Error handling
            """);

        Assert.Equal(
            [new DocHeading("routing", "Routing"), new DocHeading("error-handling", "Error handling")],
            headings);
    }

    [Theory]
    [InlineData("plain body", "plain body")]
    [InlineData("---\nnot closed", "---\nnot closed")]
    public void SplitFrontMatter_treats_malformed_headers_as_body(string input, string expectedBody)
    {
        var (front, body) = DocsLibrary.SplitFrontMatter(input);
        Assert.Empty(front);
        Assert.Equal(expectedBody, body);
    }

    [Fact]
    public void SplitFrontMatter_reads_simple_keys_and_strips_quotes()
    {
        var (front, body) = DocsLibrary.SplitFrontMatter("""
            ---
            title: "Routing"
            description: How requests reach an endpoint
            order: 30
            : skipped
            ---

            The body.
            """);

        Assert.Equal("Routing", front["title"]);
        Assert.Equal("How requests reach an endpoint", front["description"]);
        Assert.Equal("30", front["order"]);
        Assert.False(front.ContainsKey(""));
        Assert.Equal("The body.", body);
    }

    [Fact]
    public void SplitFrontMatter_normalises_crlf()
    {
        var (front, body) = DocsLibrary.SplitFrontMatter("---\r\ntitle: Hello\r\n---\r\n\r\nBody\r\n");
        Assert.Equal("Hello", front["title"]);
        Assert.Equal("Body\n", body);
    }
}
