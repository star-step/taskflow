using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using TaskFlow.Api.Data;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Options;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<SeedOptions>(config.GetSection(SeedOptions.Section));

builder.Services.AddTaskFlowDatabase(config);
builder.Services.AddTaskFlowIdentity(config);
builder.Services.AddTaskFlowAuthentication();
builder.Services.AddTaskFlowRateLimiting(config);
builder.Services.AddTaskFlowProblemDetails();
builder.Services.AddTaskFlowSwagger();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

// In Docker the API sits behind nginx; trust its X-Forwarded-* headers so rate limits see the real client IP.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var corsOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Outermost, so it records the final status after the exception handler and status-code pages ran.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(); // bodyless 401/403/404/405 become ProblemDetails too

if (config.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "TaskFlow API");
}

if (corsOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

await app.PrepareDatabaseAsync();
await app.RunAsync();

/// <summary>Exposed for WebApplicationFactory in tests.</summary>
public partial class Program;
