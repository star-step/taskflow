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
            problem.Type ??= "about:blank"; // RFC 9457 default; statuses the framework knows get an RFC 9110 link
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
        var invalid = context.ModelState
            .Where(kv => kv.Value is { ValidationState: ModelValidationState.Invalid, Errors.Count: > 0 })
            .Select(kv => (kv.Key, Entry: kv.Value!))
            .ToList();
        // Broken or missing body: the formatter's own error says it all.
        var hasJsonError = invalid.Any(i => i.Key.StartsWith("$", StringComparison.Ordinal)
            || i.Entry.Errors.Any(e => e.ErrorMessage.StartsWith("A non-empty request body is required", StringComparison.Ordinal)));

        var errors = new Dictionary<string, List<string>>();
        foreach (var (key, entry) in invalid)
        {
            var fromJson = key.StartsWith("$", StringComparison.Ordinal);

            // A body that failed to deserialise (or was empty) also leaves MVC's "The <parameter> field is required."
            // on the parameter itself; it adds nothing to the JSON error, so drop it.
            if (hasJsonError && !fromJson && entry.Errors.All(e => e.ErrorMessage.EndsWith("field is required.", StringComparison.Ordinal)))
            {
                continue;
            }

            foreach (var error in entry.Errors)
            {
                var (field, message) = Describe(NormaliseKey(key), fromJson, error);
                if (!errors.TryGetValue(field, out var list))
                {
                    errors[field] = list = new List<string>();
                }

                if (!list.Contains(message))
                {
                    list.Add(message);
                }
            }
        }

        var problem = new ValidationProblemDetails(errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more fields are invalid."
        };
        problem.Extensions["errorCode"] = ErrorCodes.ValidationFailed;

        var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        return new ProblemObjectResult(problem, problemService);
    }

    /// <summary>Picks the field and wording for one model-state error.</summary>
    private static (string Field, string Message) Describe(string field, bool fromJson, ModelError error)
    {
        var message = error.ErrorMessage;
        if (message.StartsWith(StrictEnum.MustBeOnePrefix, StringComparison.Ordinal))
        {
            return (field, $"{FieldNames.Display(field)} {message}"); // same wording as the query binder
        }

        if (fromJson && !message.StartsWith("The JSON value could not be converted", StringComparison.Ordinal))
        {
            // Not a wrong type for a field but broken JSON syntax; its path only says where parsing stopped.
            return ("request", "The request body is not valid JSON.");
        }

        return fromJson || error.Exception is not null || string.IsNullOrEmpty(message)
            ? (field, $"{FieldNames.Display(field)} has an invalid value.")
            : (field, message);
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
