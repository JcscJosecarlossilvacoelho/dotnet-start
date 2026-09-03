using DotnetStart.Content;

namespace DotnetStart.Tests;

public sealed class SkillsLibraryTests
{
    private static string Skill(string name, string description, string body) =>
        $"""
        ---
        name: {name}
        description: {description}
        ---

        # {name}

        {body}
        """;

    [Fact]
    public void Reads_every_skill_folder_in_name_order()
    {
        using var temp = new TempDocs();
        temp.Write("beta/SKILL.md", Skill("beta", "Second.", "## Two"));
        temp.Write("alpha/SKILL.md", Skill("alpha", "First.", "## One"));

        var library = new SkillsLibrary(temp.Root);

        Assert.Equal(["alpha", "beta"], library.All.Select(skill => skill.Name));
    }

    [Fact]
    public void Takes_the_name_and_description_from_the_front_matter()
    {
        using var temp = new TempDocs();
        temp.Write("csharp/SKILL.md", Skill("csharp", "Idiomatic modern C#.", "Body."));

        var skill = new SkillsLibrary(temp.Root).Find("csharp");

        Assert.NotNull(skill);
        Assert.Equal("csharp", skill.Name);
        Assert.Equal("Idiomatic modern C#.", skill.Description);
        Assert.Equal("/skills/csharp", skill.Href);
    }

    [Fact]
    public void Falls_back_to_the_folder_name_without_front_matter()
    {
        using var temp = new TempDocs();
        temp.Write("orphan/SKILL.md", "# Orphan\n\nNo front matter here.\n");

        var skill = new SkillsLibrary(temp.Root).Find("orphan");

        Assert.NotNull(skill);
        Assert.Equal("orphan", skill.Name);
        Assert.Equal(string.Empty, skill.Description);
    }

    [Fact]
    public void Renders_the_body_without_repeating_the_title()
    {
        using var temp = new TempDocs();
        temp.Write("blazor/SKILL.md", Skill("blazor", "Components.", "## Render modes\n\nPick one."));

        var skill = new SkillsLibrary(temp.Root).Find("blazor")!;

        Assert.DoesNotContain("<h1", skill.Html);
        Assert.Contains("<p>Pick one.</p>", skill.Html);
        Assert.Equal(["Render modes"], skill.Headings.Select(heading => heading.Text));
    }

    [Fact]
    public void Ignores_folders_without_a_skill_file_and_hidden_ones()
    {
        using var temp = new TempDocs();
        temp.Write("real/SKILL.md", Skill("real", "Kept.", "Body."));
        temp.Write("empty/README.md", "Not a skill.");
        temp.Write(".hidden/SKILL.md", Skill("hidden", "Dropped.", "Body."));

        var library = new SkillsLibrary(temp.Root);

        Assert.Equal(["real"], library.All.Select(skill => skill.Name));
        Assert.Null(library.Find("hidden"));
        Assert.Null(library.Find("empty"));
    }

    [Fact]
    public void Neighbours_walk_the_catalog_and_stop_at_the_ends()
    {
        using var temp = new TempDocs();
        temp.Write("a/SKILL.md", Skill("a", "A.", "Body."));
        temp.Write("b/SKILL.md", Skill("b", "B.", "Body."));
        temp.Write("c/SKILL.md", Skill("c", "C.", "Body."));

        var library = new SkillsLibrary(temp.Root);

        Assert.Equal((null, "b"), Names(library.Neighbours(library.Find("a")!)));
        Assert.Equal(("a", "c"), Names(library.Neighbours(library.Find("b")!)));
        Assert.Equal(("b", null), Names(library.Neighbours(library.Find("c")!)));

        static (string?, string?) Names((SkillEntry? Previous, SkillEntry? Next) pair)
            => (pair.Previous?.Name, pair.Next?.Name);
    }

    [Fact]
    public void A_missing_folder_is_an_empty_catalog_not_a_crash()
    {
        var library = new SkillsLibrary(Path.Combine(Path.GetTempPath(), "dotnet-start-no-skills-" + Guid.NewGuid().ToString("n")));

        Assert.Empty(library.All);
        Assert.Null(library.Find("csharp"));
    }

    [Fact]
    public void Every_published_skill_carries_the_front_matter_the_catalog_renders()
    {
        var library = new SkillsLibrary(Path.Combine(RepoRoot.Find(), "skills"));

        Assert.NotEmpty(library.All);
        Assert.All(library.All, skill =>
        {
            Assert.NotEmpty(skill.Description);
            Assert.NotEmpty(skill.Html);
            Assert.Contains($"--skill {skill.Name}", skill.InstallCommand);
            Assert.StartsWith("skills/", skill.RelativePath);
        });
    }
}
