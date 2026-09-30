using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Errors;
using Testcontainers.MsSql;

namespace TaskFlow.Api.Tests.Integration;

/// <summary>A real SQL Server unique-index violation is a 409 conflict, not "database unavailable".</summary>
[Trait("Category", "Integration")]
public sealed class SqlErrorMappingTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private string? _skipReason;

    public async Task InitializeAsync() => (_sql, _skipReason) = await SqlServerContainer.StartAsync();

    public async Task DisposeAsync()
    {
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task UniqueViolation_BecomesConflict()
    {
        Skip.If(_skipReason is not null, _skipReason);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_sql!.TaskFlowDatabase()).Options;
        await using var db = new AppDbContext(options, new FakeTimeProvider(DateTimeOffset.UtcNow));
        await db.Database.MigrateAsync();

        var user = new AppUser { UserName = "u@company.com", NormalizedUserName = "U@COMPANY.COM", Email = "u@company.com", NormalizedEmail = "U@COMPANY.COM", DisplayName = "U", SecurityStamp = "s" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var hash = new string('a', 64);
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = hash, SecurityStamp = "s", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = hash, SecurityStamp = "s", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        var violation = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var services = new ServiceCollection().AddLogging().AddTaskFlowProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
        var handler = new GlobalExceptionHandler(services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance, new HostingEnvironmentStub());
        await handler.TryHandleAsync(context, violation, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = JsonDocument.Parse(context.Response.Body).RootElement;
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("DUPLICATE_VALUE", body.GetProperty("errorCode").GetString());
    }

    private sealed class HostingEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "TaskFlow.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
