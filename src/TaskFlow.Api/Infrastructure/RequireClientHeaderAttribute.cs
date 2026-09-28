using Microsoft.AspNetCore.Mvc.Filters;
using TaskFlow.Api.Errors;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// CSRF defence for the cookie-authenticated endpoints (/refresh, /logout). A cross-site HTML form
/// can't set custom headers, so requiring one proves the request came from our own script.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireClientHeaderAttribute : Attribute, IResourceFilter
{
    public const string Header = "X-TaskFlow-Client";

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey(Header))
        {
            throw new ClientHeaderMissingException(Header);
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
