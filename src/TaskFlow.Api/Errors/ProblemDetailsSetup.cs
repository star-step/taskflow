using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Api.Infrastructure;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Makes every error response share one shape: model-binding failures, auth challenges (401/403),
/// rate limiting (429) and exceptions all return ProblemDetails with errorCode and traceId.
/// </summary>
public static class ProblemDetailsSetup
{
    public static IServiceCollection AddTaskFlowProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
        {
            var problem = ctx.ProblemDetails;
            problem.Instance ??= ctx.HttpContext.Request.Path;
            problem.Type ??= $"https://httpstatuses.io/{problem.Status}";
            problem.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
            if (!problem.Extensions.ContainsKey("errorCode"))
            {
                problem.Extensions["errorCode"] = ErrorCodes.ForStatus(problem.Status ?? 500);
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // PostConfigure: AddControllers() registers MVC's own factory, which would otherwise replace ours.
        services.PostConfigure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ValidationResponse);
        return services;
    }

    /// <summary>
    /// Builds the 400 body for invalid model state. Keys are normalised to camelCase field names
    /// ("Title" and "$.title" both become "title"), and raw JSON converter messages are replaced
    /// with a readable one.
    /// </summary>
    private static IActionResult ValidationResponse(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.ValidationState != ModelValidationState.Invalid || entry.Errors.Count == 0)
            {
                continue;
            }

            var field = NormaliseKey(key);
            var fromJson = key.StartsWith("$", StringComparison.Ordinal);
            var messages = entry.Errors
                .Select(e => e.ErrorMessage.StartsWith(StrictEnum.MustBeOnePrefix, StringComparison.Ordinal)
                    ? $"{FieldNames.Display(field)} {e.ErrorMessage}" // enum in a JSON body: same wording as the query binder
                    : fromJson || e.Exception is not null || string.IsNullOrEmpty(e.ErrorMessage)
                        ? $"{FieldNames.Display(field)} has an invalid value."
                        : e.ErrorMessage)
                .Distinct()
                .ToArray();
            errors[field] = errors.TryGetValue(field, out var existing) ? existing.Concat(messages).Distinct().ToArray() : messages;
        }

        // When the body itself can't be parsed, MVC also adds a generic "request field is required" error.
        if (errors.Count > 1)
        {
            errors.Remove("request");
        }

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more fields are invalid."
        };
        problem.Extensions["errorCode"] = ErrorCodes.ValidationFailed;

        var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        return new ProblemObjectResult(problem, problemService);
    }

    private static string NormaliseKey(string key)
    {
        var name = key.TrimStart('$', '.');
        if (name.Length == 0)
        {
            return "request";
        }

        return JsonNamingPolicy.CamelCase.ConvertName(name);
    }


    /// <summary>Writes through IProblemDetailsService so CustomizeProblemDetails (traceId etc.) runs.</summary>
    private sealed class ProblemObjectResult : IActionResult
    {
        private readonly ProblemDetails _problem;
        private readonly IProblemDetailsService _service;

        public ProblemObjectResult(ProblemDetails problem, IProblemDetailsService service)
        {
            _problem = problem;
            _service = service;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = _problem.Status!.Value;
            await _service.WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = _problem });
        }
    }
}
