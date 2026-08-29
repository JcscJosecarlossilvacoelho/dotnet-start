namespace DotnetSexy.Docs;

/// <summary>A single Markdown document under <c>docs/</c>.</summary>
public sealed record DocEntry(
    string Slug,
    string SectionId,
    string Title,
    string Description,
    int Order,
    string RelativePath,
    string Markdown)
{
    public string Href => $"/docs/{Slug}";
    public string EditUrl => $"https://github.com/dotnet-sexy/dotnet-sexy/edit/main/{RelativePath}";

    /// <summary>Position in the flattened reading order, 1-based. Assigned when the library loads.</summary>
    public int Number { get; init; }

    /// <summary>The rendered body, built once when the library loads.</summary>
    public string Html { get; init; } = "";

    /// <summary>Level-2 headings of <see cref="Html"/>, built once when the library loads.</summary>
    public IReadOnlyList<DocHeading> Headings { get; init; } = [];

    /// <summary>Lower-cased title, description, slug and section — what a search matches first.</summary>
    public string TitleHaystack { get; init; } = "";

    /// <summary>Lower-cased everything, including the body — what a search falls back to.</summary>
    public string BodyHaystack { get; init; } = "";
}

/// <summary>A folder under <c>docs/</c>, described by its optional <c>_section.md</c>.</summary>
public sealed record DocSection(
    string Id,
    string Title,
    string Description,
    int Order,
    IReadOnlyList<DocEntry> Documents)
{
    /// <summary>Uppercased title, precomputed for the navigation and index headings.</summary>
    public string Label { get; init; } = Title.ToUpperInvariant();
}

/// <summary>A level-2 heading of a rendered document.</summary>
public sealed record DocHeading(string Id, string Text);
