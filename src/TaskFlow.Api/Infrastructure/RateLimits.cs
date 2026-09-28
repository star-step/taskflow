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

    public static IServiceCollection AddTaskFlowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            AddPerIp(options, Login, permits: 10, window: TimeSpan.FromMinutes(1));
            AddPerIp(options, Register, permits: 5, window: TimeSpan.FromMinutes(10));
            AddPerIp(options, Refresh, permits: 30, window: TimeSpan.FromMinutes(1));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (ctx, ct) =>
            {
                var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : TimeSpan.FromMinutes(1);
                var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                ctx.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

                var problem = new ProblemDetails
                {
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
