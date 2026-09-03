namespace DotnetStart.Content;

/// <summary>
/// Reads the agent skills in <c>skills/</c> and renders each <c>SKILL.md</c> for the
/// site. The catalog links here so a reader can see what a skill actually tells the
/// agent before installing it — the file itself, not the guide it points at.
/// </summary>
/// <remarks>
/// Unlike <see cref="DocsLibrary"/> there is no rescan and no locking: eight files
/// are cheap enough to read in the constructor, and a singleton built once at
/// startup is then immutable and safe to share without any publication ceremony.
/// Editing a <c>SKILL.md</c> needs a restart, which is what <c>dotnet watch</c> does.
/// </remarks>
public sealed class SkillsLibrary
{
    private const string SkillFileName = "SKILL.md";

    private readonly List<SkillEntry> _ordered;
    private readonly Dictionary<string, SkillEntry> _byName;

    public SkillsLibrary(IWebHostEnvironment environment)
        : this(Path.Combine(environment.ContentRootPath, "skills"))
    {
    }

    public SkillsLibrary(string root)
    {
        _ordered = Load(root);
        _byName = _ordered.ToDictionary(skill => skill.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SkillEntry> All => _ordered;

    public SkillEntry? Find(string? name)
        => !string.IsNullOrWhiteSpace(name) && _byName.TryGetValue(name, out var skill) ? skill : null;

    public (SkillEntry? Previous, SkillEntry? Next) Neighbours(SkillEntry skill)
    {
        var index = _ordered.FindIndex(candidate => candidate.Name == skill.Name);
        if (index < 0) return (null, null);
        return (index > 0 ? _ordered[index - 1] : null,
                index < _ordered.Count - 1 ? _ordered[index + 1] : null);
    }

    private static List<SkillEntry> Load(string root)
    {
        var skills = new List<SkillEntry>();
        if (!Directory.Exists(root)) return skills;

        foreach (var directory in Directory.EnumerateDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(directory);
            if (name.StartsWith('.') || name.StartsWith('_')) continue;

            var file = Path.Combine(directory, SkillFileName);
            if (File.Exists(file)) skills.Add(Read(root, file, name));
        }

        return skills;
    }

    /// <summary>Renders the body and pulls the headings once, here, not per request.</summary>
    private static SkillEntry Read(string root, string file, string directoryName)
    {
        var (front, body) = DocsLibrary.SplitFrontMatter(File.ReadAllText(file));
        var relative = Path.GetRelativePath(Path.GetDirectoryName(root)!, file).Replace('\\', '/');

        return new SkillEntry(
            Name: front.GetValueOrDefault("name", directoryName),
            Description: front.GetValueOrDefault("description", string.Empty),
            RelativePath: relative,
            Markdown: body)
        {
            Html = DocsLibrary.ToHtml(body),
            Headings = DocsLibrary.Headings(body),
        };
    }
}
