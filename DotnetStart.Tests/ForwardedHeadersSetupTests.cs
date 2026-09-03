using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

using DotnetStart.Hosting;

namespace DotnetStart.Tests;

[Collection("Environment")]
public sealed class ForwardedHeadersSetupTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsTruthy_accepts_the_usual_opt_in_spellings(string? value, bool expected)
        => Assert.Equal(expected, ForwardedHeadersSetup.IsTruthy(value));

    [Fact]
    public void Apply_does_not_clear_the_default_loopback_allow_list()
    {
        var options = new ForwardedHeadersOptions();
        var networksBefore = options.KnownIPNetworks.Count;
        var proxiesBefore = options.KnownProxies.Count;
        Assert.True(networksBefore > 0);

        ForwardedHeadersSetup.Apply(options);

        Assert.Equal(networksBefore, options.KnownIPNetworks.Count);
        Assert.Equal(proxiesBefore, options.KnownProxies.Count);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(1, options.ForwardLimit);
    }

    [Fact]
    public void Apply_adds_configured_networks_without_trusting_the_whole_internet()
    {
        using var env = new Env(
            (ForwardedHeadersSetup.KnownNetworksVariable, "10.0.0.0/8"),
            (ForwardedHeadersSetup.KnownProxiesVariable, "127.0.0.1"));

        var options = new ForwardedHeadersOptions();
        var loopbackNetworks = options.KnownIPNetworks.Count;

        ForwardedHeadersSetup.Apply(options);

        Assert.Equal(loopbackNetworks + 1, options.KnownIPNetworks.Count);
        Assert.Contains(options.KnownIPNetworks, network => network.BaseAddress.Equals(IPAddress.Parse("10.0.0.0")) && network.PrefixLength == 8);
        Assert.Contains(IPAddress.Parse("127.0.0.1"), options.KnownProxies);
    }

    [Fact]
    public void ParseNetworks_rejects_an_invalid_cidr()
        => Assert.Throws<InvalidOperationException>(() =>
            ForwardedHeadersSetup.ParseNetworks("not-a-cidr").ToList());

    [Fact]
    public void ParseProxies_rejects_an_invalid_ip()
        => Assert.Throws<InvalidOperationException>(() =>
            ForwardedHeadersSetup.ParseProxies("host.example").ToList());

    [Fact]
    public void PrivateEdgeNetworks_is_a_parseable_allow_list()
    {
        var networks = ForwardedHeadersSetup.ParseNetworks(ForwardedHeadersSetup.PrivateEdgeNetworks).ToList();
        Assert.Equal(5, networks.Count);
        Assert.DoesNotContain(networks, network => network.PrefixLength == 0);
    }

    [Fact]
    public void IsEnabled_reads_the_opt_in_variable()
    {
        using var enabled = new Env((ForwardedHeadersSetup.EnabledVariable, "true"));
        Assert.True(ForwardedHeadersSetup.IsEnabled());

        using var disabled = new Env((ForwardedHeadersSetup.EnabledVariable, null));
        Assert.False(ForwardedHeadersSetup.IsEnabled());
    }

    private sealed class Env : IDisposable
    {
        private readonly (string Key, string? Previous)[] _restore;

        public Env(params (string Key, string? Value)[] pairs)
        {
            _restore = pairs.Select(pair => (pair.Key, Environment.GetEnvironmentVariable(pair.Key))).ToArray();
            foreach (var (key, value) in pairs)
                Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose()
        {
            foreach (var (key, previous) in _restore)
                Environment.SetEnvironmentVariable(key, previous);
        }
    }
}

[CollectionDefinition("Environment", DisableParallelization = true)]
public sealed class EnvironmentCollection;
