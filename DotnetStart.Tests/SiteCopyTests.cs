namespace DotnetStart.Tests;

public sealed class SiteCopyTests
{
    public const string InstallCommand = "npx skills add JcscJosecarlossilvacoelho/dotnet-start";

    [Fact]
    public void Preview_homepage_uses_the_same_install_command_as_the_Blazor_site()
    {
        var root = RepoRoot.Find();
        var preview = File.ReadAllText(Path.Combine(root, "app", "page.tsx"));
        var skills = File.ReadAllText(Path.Combine(root, "components", "Pages", "Skills.razor"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.Contains(InstallCommand, preview);
        Assert.Contains(InstallCommand, skills);
        Assert.Contains(InstallCommand, readme);
        Assert.DoesNotMatch(@"npx skills add (?!JcscJosecarlossilvacoelho/)dotnet-start", preview);
    }

    [Fact]
    public void Platform_blueprints_opt_in_to_forwarded_headers_with_private_networks()
    {
        var root = RepoRoot.Find();
        var render = File.ReadAllText(Path.Combine(root, "render.yaml"));
        var fly = File.ReadAllText(Path.Combine(root, "fly.toml"));

        Assert.Contains("FORWARDED_HEADERS_ENABLED", render);
        Assert.Contains("FORWARDED_HEADERS_ENABLED", fly);
        Assert.Contains(ForwardedHeadersSetup.PrivateEdgeNetworks, render);
        Assert.Contains(ForwardedHeadersSetup.PrivateEdgeNetworks, fly);
    }
}
