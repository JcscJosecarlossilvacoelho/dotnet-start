namespace DotnetStart.Docs;

/// <summary>A single agent skill: the <c>SKILL.md</c> under <c>skills/&lt;name&gt;/</c>.</summary>
public sealed record SkillEntry(
    string Name,
    string Description,
    string RelativePath,
    string Markdown)
{
    public string Href => $"/skills/{Name}";
    public string EditUrl => $"https://github.com/JcscJosecarlossilvacoelho/dotnet-start/blob/main/{RelativePath}";
    public string InstallCommand => $"npx skills add JcscJosecarlossilvacoelho/dotnet-start --skill {Name}";

    /// <summary>The rendered body, built once when the library loads.</summary>
    public string Html { get; init; } = "";

    /// <summary>Level-2 headings of <see cref="Html"/>, built once when the library loads.</summary>
    public IReadOnlyList<DocHeading> Headings { get; init; } = [];
}
