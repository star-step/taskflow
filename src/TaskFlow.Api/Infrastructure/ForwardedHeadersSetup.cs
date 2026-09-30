using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// Which proxies may set X-Forwarded-For/Proto. Rate limits partition by client IP, so trusting the
/// header from anyone would let a caller pick a new "IP" per request and bypass them.
/// Trusted: loopback (framework default) plus ForwardedHeaders:KnownProxies (IP addresses) and
/// ForwardedHeaders:KnownNetworks (CIDR, e.g. "172.30.89.0/24"). In Docker that is only nginx.
/// </summary>
public static class ForwardedHeadersSetup
{
    public static void Configure(ForwardedHeadersOptions o, IConfiguration config)
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Only the last hop counts: nginx appends the real client address to whatever the client sent.
        o.ForwardLimit = 1;

        foreach (var proxy in config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
        {
            o.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var cidr in config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>())
        {
            var parts = cidr.Split('/');
            o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
        }
    }
}
