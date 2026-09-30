using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Api.Errors;

namespace TaskFlow.Api.Infrastructure;

/// <summary>Per-IP limits on the anonymous account endpoints, to slow down password guessing and sign-up spam.</summary>
public static class RateLimits
{
    public const string Login = "login";
    public const string Register = "register";
    public const string Refresh = "refresh";

    /// <summary>Permits per window, overridable under "RateLimits" in configuration.</summary>
    public sealed class Settings
    {
        public int LoginPerMinute { get; set; } = 10;
        public int RegisterPerTenMinutes { get; set; } = 5;
        public int RefreshPerMinute { get; set; } = 30;
    }

    public static IServiceCollection AddTaskFlowRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var limits = config.GetSection("RateLimits").Get<Settings>() ?? new Settings();
        services.AddRateLimiter(options =>
        {
            AddPerIp(options, Login, limits.LoginPerMinute, TimeSpan.FromMinutes(1));
            AddPerIp(options, Register, limits.RegisterPerTenMinutes, TimeSpan.FromMinutes(10));
            AddPerIp(options, Refresh, limits.RefreshPerMinute, TimeSpan.FromMinutes(1));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (ctx, ct) =>
            {
                var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : TimeSpan.FromMinutes(1);
                var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                ctx.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

                var problem = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc6585#section-4", // 429 is defined in RFC 6585, not RFC 9110
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many attempts",
                    Detail = seconds >= 60
                        ? $"Too many attempts. Try again in {Math.Ceiling(seconds / 60.0)} minute(s)."
                        : $"Too many attempts. Try again in {seconds} seconds."
                };
                problem.Extensions["errorCode"] = ErrorCodes.RateLimited;
                var writer = ctx.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await writer.WriteAsync(new ProblemDetailsContext { HttpContext = ctx.HttpContext, ProblemDetails = problem });
            };
        });
        return services;
    }

    private static void AddPerIp(RateLimiterOptions options, string policy, int permits, TimeSpan window) =>
        options.AddPolicy(policy, http => RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));
}
