using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace DotnetStart;

/// <summary>
/// Trusts <c>X-Forwarded-*</c> only when operators opt in and name the proxies they
/// actually have. Presence of <c>PORT</c> is not a trust decision — platforms inject
/// that for binding — so the default loopback allow-list is never cleared implicitly.
/// </summary>
internal static class ForwardedHeadersSetup
{
    public const string EnabledVariable = "FORWARDED_HEADERS_ENABLED";
    public const string KnownNetworksVariable = "FORWARDED_HEADERS_KNOWN_NETWORKS";
    public const string KnownProxiesVariable = "FORWARDED_HEADERS_KNOWN_PROXIES";

    /// <summary>
    /// RFC1918, CGNAT and IPv6 unique-local — the ranges a typical PaaS load
    /// balancer originates from. Referenced by <c>render.yaml</c> and <c>fly.toml</c>;
    /// never applied just because <c>PORT</c> is set.
    /// </summary>
    public const string PrivateEdgeNetworks =
        "10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,100.64.0.0/10,fc00::/7";

    public static bool IsEnabled() => IsTruthy(Environment.GetEnvironmentVariable(EnabledVariable));

    public static void Apply(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;

        foreach (var network in ParseNetworks(Environment.GetEnvironmentVariable(KnownNetworksVariable)))
        {
            options.KnownIPNetworks.Add(network);
        }

        foreach (var proxy in ParseProxies(Environment.GetEnvironmentVariable(KnownProxiesVariable)))
        {
            options.KnownProxies.Add(proxy);
        }
    }

    internal static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    internal static IEnumerable<IPNetwork> ParseNetworks(string? raw)
    {
        foreach (var token in Split(raw))
        {
            if (!IPNetwork.TryParse(token, out var network))
            {
                throw new InvalidOperationException(
                    $"Invalid CIDR in {KnownNetworksVariable}: '{token}'.");
            }

            yield return network;
        }
    }

    internal static IEnumerable<IPAddress> ParseProxies(string? raw)
    {
        foreach (var token in Split(raw))
        {
            if (!IPAddress.TryParse(token, out var address))
            {
                throw new InvalidOperationException(
                    $"Invalid IP in {KnownProxiesVariable}: '{token}'.");
            }

            yield return address;
        }
    }

    private static IEnumerable<string> Split(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) yield break;

        foreach (var token in raw.Split([',', ';', ' ', '\t', '\n', '\r'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return token;
        }
    }
}
