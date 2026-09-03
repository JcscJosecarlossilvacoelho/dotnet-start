using DotnetStart.Hosting;

namespace DotnetStart.Tests;

public sealed class SiteCopyTests
{
    public const string InstallCommand = "npx skills add JcscJosecarlossilvacoelho/dotnet-start";

    [Fact]
    public void The_catalog_and_the_readme_advertise_the_same_install_command()
    {
        var root = RepoRoot.Find();
        var skills = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Skills.razor"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.Contains(InstallCommand, skills);
        Assert.Contains(InstallCommand, readme);
        Assert.DoesNotMatch(@"npx skills add (?!JcscJosecarlossilvacoelho/)dotnet-start", skills);
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
