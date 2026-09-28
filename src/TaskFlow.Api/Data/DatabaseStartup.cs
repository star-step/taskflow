using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Options;

namespace TaskFlow.Api.Data;

public static class DatabaseStartup
{
    /// <summary>
    /// Applies pending migrations (when Database:MigrateOnStartup is true) and creates demo users
    /// (when Seed:DemoUsers is true). Both are meant for local and Docker environments; production
    /// should run an EF migration bundle as a deployment step instead.
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseStartup));

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            var db = services.GetRequiredService<AppDbContext>();
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied");
                    break;
                }
                catch (Exception ex) when (attempt < 10)
                {
                    logger.LogWarning(ex, "Database not ready (attempt {Attempt}); retrying in 5s", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        var seed = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (seed.DemoUsers)
        {
            await SeedDemoUsersAsync(services.GetRequiredService<UserManager<AppUser>>(),
                services.GetRequiredService<AppDbContext>(), seed.Users, logger);
        }
    }

    private static async Task SeedDemoUsersAsync(UserManager<AppUser> users, AppDbContext db,
        IEnumerable<SeedOptions.DemoUser> demoUsers, ILogger logger)
    {
        foreach (var demo in demoUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email) && !string.IsNullOrEmpty(u.Password)))
        {
            if (await users.FindByEmailAsync(demo.Email) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                UserName = demo.Email,
                Email = demo.Email,
                DisplayName = demo.DisplayName,
                CreatedDate = db.UtcNow(),
                LockoutEnabled = true
            };
            var result = await users.CreateAsync(user, demo.Password);
            if (result.Succeeded)
            {
                logger.LogInformation("Created demo user {Email}", demo.Email);
            }
            else
            {
                logger.LogWarning("Could not create demo user {Email}: {Errors}", demo.Email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
