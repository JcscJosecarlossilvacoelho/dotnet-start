namespace DotnetStart.Docs;

/// <summary>
/// Reads the agent skills in <c>skills/</c> and renders each <c>SKILL.md</c> for the
/// site. The catalog page links here so a reader can see what a skill actually tells
/// the agent before installing it — the file itself, not the guide it points at.
/// </summary>
public sealed class SkillsLibrary
{
    private const string SkillFileName = "SKILL.md";

    private readonly string _root;
    private readonly object _gate = new();
    private Dictionary<string, SkillEntry> _byName = new(StringComparer.OrdinalIgnoreCase);
    private List<SkillEntry> _ordered = [];

    public SkillsLibrary(IWebHostEnvironment environment)
        : this(Path.Combine(environment.ContentRootPath, "skills"))
    {
    }

    public SkillsLibrary(string root) => _root = root;

    /// <summary>Reads and renders everything up front, so no visitor pays for the parse.</summary>
    public void Warm() => EnsureLoaded();

    public IReadOnlyList<SkillEntry> All { get { EnsureLoaded(); return _ordered; } }

    public SkillEntry? Find(string? name)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _byName.TryGetValue(name.Trim('/'), out var skill) ? skill : null;
    }

    private void EnsureLoaded()
    {
        if (_ordered.Count > 0) return;

        lock (_gate)
        {
            if (_ordered.Count > 0) return;
            Load();
        }
    }

    private void Load()
    {
        if (!Directory.Exists(_root)) return;

        var skills = new List<SkillEntry>();

        foreach (var directory in Directory.EnumerateDirectories(_root).OrderBy(d => d, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(directory);
            if (name.StartsWith('.') || name.StartsWith('_')) continue;

            var file = Path.Combine(directory, SkillFileName);
            if (!File.Exists(file)) continue;

            skills.Add(Read(file, name));
        }

        _ordered = skills;
        _byName = skills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
    }

    private SkillEntry Read(string file, string directoryName)
    {
        var (front, body) = DocsLibrary.SplitFrontMatter(File.ReadAllText(file));
        var relative = Path.GetRelativePath(Path.GetDirectoryName(_root)!, file).Replace('\\', '/');

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
