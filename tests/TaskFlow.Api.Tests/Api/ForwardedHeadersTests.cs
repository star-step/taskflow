using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Api.Infrastructure;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace TaskFlow.Api.Tests.Api;

/// <summary>
/// X-Forwarded-For is only honoured from configured proxies (nginx). Otherwise any client could pick
/// a new "IP" per request and get a fresh rate-limit partition on login/register/refresh.
/// </summary>
public sealed class ForwardedHeadersTests
{
    private const string Nginx = "172.30.89.10";

    private static async Task<IPAddress?> ClientIpSeenByApi(string remoteIp, string forwardedFor, bool nginxConfigured = true)
    {
        var settings = new Dictionary<string, string?>();
        if (nginxConfigured)
        {
            settings["ForwardedHeaders:KnownProxies:0"] = Nginx;
        }

        var options = new ForwardedHeadersOptions();
        ForwardedHeadersSetup.Configure(options, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        IPAddress? seen = null;
        var middleware = new ForwardedHeadersMiddleware(ctx => { seen = ctx.Connection.RemoteIpAddress; return Task.CompletedTask; },
            NullLoggerFactory.Instance, MsOptions.Create(options));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await middleware.Invoke(context);
        return seen;
    }

    [Fact]
    public async Task UntrustedCaller_CannotSpoofItsAddress()
    {
        Assert.Equal(IPAddress.Parse("172.30.89.20"), await ClientIpSeenByApi("172.30.89.20", "203.0.113.7"));
    }

    [Fact]
    public async Task Nginx_ForwardsTheRealClientAddress()
    {
        Assert.Equal(IPAddress.Parse("198.51.100.4"), await ClientIpSeenByApi(Nginx, "198.51.100.4"));
    }

    [Fact]
    public async Task ThroughNginx_OnlyTheEntryNginxAppendedCounts()
    {
        // The client sent "X-Forwarded-For: 203.0.113.7"; nginx appended the real address.
        Assert.Equal(IPAddress.Parse("198.51.100.4"), await ClientIpSeenByApi(Nginx, "203.0.113.7, 198.51.100.4"));
    }

    [Fact]
    public async Task WithoutConfiguration_OnlyLoopbackIsTrusted()
    {
        Assert.Equal(IPAddress.Parse("10.0.0.5"), await ClientIpSeenByApi("10.0.0.5", "203.0.113.7", nginxConfigured: false));
        Assert.Equal(IPAddress.Parse("198.51.100.4"), await ClientIpSeenByApi("127.0.0.1", "198.51.100.4", nginxConfigured: false));
    }
}
