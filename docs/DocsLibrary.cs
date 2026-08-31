using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace DotnetStart.Docs;

/// <summary>
/// Reads the community-owned Markdown in <c>docs/</c> and turns it into the
/// navigation, pages, and search index the site renders. Content is never
/// hardcoded in a component: adding a Markdown file is enough to publish a page.
/// </summary>
public sealed class DocsLibrary
{
    private const string SectionFileName = "_section.md";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private readonly string _root;
    private readonly object _gate = new();
    private List<DocSection> _sections = [];
    private Dictionary<string, DocEntry> _bySlug = new(StringComparer.OrdinalIgnoreCase);
    private List<DocEntry> _ordered = [];
    private DateTime _loadedFrom = DateTime.MinValue;
    private long _nextScanTicks;

    /// <summary>
    /// How long a load is trusted before the folder is stat-ed again. Reading 80+ files
    /// off disk on every property access dwarfs the cost of rendering a page, so in
    /// production the content is treated as immutable and never rescanned.
    /// </summary>
    private readonly TimeSpan _rescanAfter;

    public DocsLibrary(IWebHostEnvironment environment)
    {
        _root = Path.Combine(environment.ContentRootPath, "docs");
        _rescanAfter = environment.IsDevelopment() ? TimeSpan.FromSeconds(1) : Timeout.InfiniteTimeSpan;
    }

    public IReadOnlyList<DocSection> Sections { get { EnsureLoaded(); return _sections; } }

    public IReadOnlyList<DocEntry> AllDocuments { get { EnsureLoaded(); return _ordered; } }

    public DocEntry? Find(string? slug)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(slug)) return null;
        return _bySlug.TryGetValue(slug.Trim('/'), out var doc) ? doc : null;
    }

    public DocSection? FindSection(string id)
        => Sections.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public (DocEntry? Previous, DocEntry? Next) Neighbours(DocEntry doc)
    {
        EnsureLoaded();
        var index = _ordered.FindIndex(d => d.Slug == doc.Slug);
        if (index < 0) return (null, null);
        return (index > 0 ? _ordered[index - 1] : null,
                index < _ordered.Count - 1 ? _ordered[index + 1] : null);
    }

    /// <summary>Documents whose title, description, slug, or body match every word of the query.</summary>
    public IReadOnlyList<DocEntry> Search(string? query, int take = 40)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(query)) return _ordered.Take(take).ToList();

        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var titleHits = new List<DocEntry>();
        var bodyHits = new List<DocEntry>();

        // The haystacks are already lower-cased, so this is an ordinal scan with no
        // per-comparison culture work and no string concatenation per document.
        foreach (var doc in _ordered)
        {
            if (Matches(doc.TitleHaystack, words)) titleHits.Add(doc);
            else if (Matches(doc.BodyHaystack, words)) bodyHits.Add(doc);

            if (titleHits.Count >= take) break;
        }

        if (titleHits.Count >= take) return titleHits.GetRange(0, take);
        if (titleHits.Count + bodyHits.Count <= take)
        {
            titleHits.AddRange(bodyHits);
            return titleHits;
        }

        titleHits.AddRange(bodyHits.GetRange(0, take - titleHits.Count));
        return titleHits;
    }

    private static bool Matches(string haystack, string[] words)
    {
        foreach (var word in words)
        {
            if (!haystack.Contains(word, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    public static string ToHtml(string markdown) => Markdown.ToHtml(StripLeadingH1(markdown), Pipeline);

    /// <summary>Front-matter titles are rendered by the page chrome, so drop a duplicate H1.</summary>
    public static string StripLeadingH1(string markdown)
    {
        var normalised = markdown.Replace("\r\n", "\n");
        if (!normalised.StartsWith("# ", StringComparison.Ordinal)) return normalised;
        var newline = normalised.IndexOf('\n');
        if (newline < 0) return string.Empty;
        return normalised[(newline + 1)..].TrimStart('\n');
    }

    /// <summary>Level-2 headings, used to build the "on this page" rail.</summary>
    public static IReadOnlyList<DocHeading> Headings(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        return document
            .Descendants<HeadingBlock>()
            .Where(h => h.Level == 2 && h.Inline is not null)
            .Select(h => new DocHeading(
                h.GetAttributes().Id ?? string.Empty,
                string.Concat(h.Inline!.Descendants<Markdig.Syntax.Inlines.LiteralInline>().Select(l => l.Content.ToString()))))
            .Where(h => h.Id.Length > 0 && h.Text.Length > 0)
            .ToList();
    }

    private void EnsureLoaded()
    {
        // The fast path — every page render after the first — is a single volatile read.
        if (_ordered.Count > 0 && Environment.TickCount64 < Interlocked.Read(ref _nextScanTicks)) return;
        if (!Directory.Exists(_root)) return;

        var newest = Directory
            .EnumerateFiles(_root, "*.md", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        ScheduleNextScan();

        if (newest <= _loadedFrom && _ordered.Count > 0) return;

        lock (_gate)
        {
            if (newest <= _loadedFrom && _ordered.Count > 0) return;
            Load();
            _loadedFrom = newest;
        }
    }

    private void ScheduleNextScan()
    {
        var next = _rescanAfter == Timeout.InfiniteTimeSpan
            ? long.MaxValue
            : Environment.TickCount64 + (long)_rescanAfter.TotalMilliseconds;
        Interlocked.Exchange(ref _nextScanTicks, next);
    }

    private void Load()
    {
        var sections = new List<DocSection>();

        foreach (var directory in Directory.EnumerateDirectories(_root).OrderBy(d => d))
        {
            var id = Path.GetFileName(directory);
            if (id.StartsWith('.') || id.StartsWith('_')) continue;

            var (sectionTitle, sectionDescription, sectionOrder) = ReadSectionMeta(directory, id);
            var documents = new List<DocEntry>();

            foreach (var file in Directory.EnumerateFiles(directory, "*.md").OrderBy(f => f))
            {
                if (Path.GetFileName(file) == SectionFileName) continue;
                documents.Add(ReadDocument(file, id));
            }

            if (documents.Count == 0) continue;

            sections.Add(new DocSection(
                id,
                sectionTitle,
                sectionDescription,
                sectionOrder,
                documents.OrderBy(d => d.Order).ThenBy(d => d.Title).ToList()));
        }

        var ordered = sections
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title)
            .SelectMany(s => s.Documents)
            .ToList();

        // Render the Markdown, pull the headings and build the search haystacks once,
        // here, instead of on every request that touches the document.
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i] = Prepare(ordered[i], i + 1);
        }

        var bySlug = ordered.ToDictionary(d => d.Slug, StringComparer.OrdinalIgnoreCase);

        _sections = sections
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title)
            .Select(s => s with { Documents = s.Documents.Select(d => bySlug[d.Slug]).ToList() })
            .ToList();
        _ordered = ordered;
        _bySlug = bySlug;
    }

    /// <summary>Attaches everything a page or a search needs, so nothing is recomputed per request.</summary>
    private static DocEntry Prepare(DocEntry doc, int number)
    {
        var body = StripLeadingH1(doc.Markdown);
        var titleHaystack = $"{doc.Title} {doc.Description} {doc.Slug}".ToLowerInvariant();

        return doc with
        {
            Number = number,
            Html = Markdown.ToHtml(body, Pipeline),
            Headings = Headings(doc.Markdown),
            TitleHaystack = titleHaystack,
            BodyHaystack = $"{titleHaystack} {doc.SectionId} {doc.Markdown}".ToLowerInvariant(),
        };
    }

    private (string Title, string Description, int Order) ReadSectionMeta(string directory, string id)
    {
        var file = Path.Combine(directory, SectionFileName);
        if (!File.Exists(file)) return (Humanise(id), string.Empty, 500);

        var (front, _) = SplitFrontMatter(File.ReadAllText(file));
        return (front.GetValueOrDefault("title", Humanise(id)),
                front.GetValueOrDefault("description", string.Empty),
                ParseOrder(front));
    }

    private DocEntry ReadDocument(string file, string sectionId)
    {
        var (front, body) = SplitFrontMatter(File.ReadAllText(file));
        var name = Path.GetFileNameWithoutExtension(file);
        var relative = Path.GetRelativePath(Path.GetDirectoryName(_root)!, file).Replace('\\', '/');

        return new DocEntry(
            Slug: $"{sectionId}/{name}",
            SectionId: sectionId,
            Title: front.GetValueOrDefault("title", Humanise(name)),
            Description: front.GetValueOrDefault("description", string.Empty),
            Order: ParseOrder(front),
            RelativePath: relative,
            Markdown: body);
    }

    private static int ParseOrder(IReadOnlyDictionary<string, string> front)
        => front.TryGetValue("order", out var raw) && int.TryParse(raw, out var order) ? order : 500;

    /// <summary>Splits a `---` delimited YAML-ish header of simple `key: value` pairs from the body.</summary>
    private static (Dictionary<string, string> Front, string Body) SplitFrontMatter(string text)
    {
        var front = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalised = text.Replace("\r\n", "\n");
        if (!normalised.StartsWith("---\n", StringComparison.Ordinal)) return (front, normalised);

        var end = normalised.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0) return (front, normalised);

        foreach (var line in normalised[4..end].Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0) front[key] = value;
        }

        var bodyStart = normalised.IndexOf('\n', end + 1);
        return (front, bodyStart < 0 ? string.Empty : normalised[(bodyStart + 1)..].TrimStart('\n'));
    }

    private static string Humanise(string value)
    {
        var words = value.Replace('-', ' ').Replace('_', ' ').Trim();
        return words.Length == 0 ? value : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
