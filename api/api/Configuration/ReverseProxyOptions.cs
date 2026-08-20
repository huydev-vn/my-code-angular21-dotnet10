namespace Api.Configuration;

/// <summary>
/// Trusted reverse-proxy addresses used to honor X-Forwarded-* headers.
/// Leave empty in Development; set production proxy IPs or CIDR ranges explicitly.
/// </summary>
internal sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownNetworks { get; init; } = [];
}
