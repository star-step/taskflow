using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Data;

/// <summary>
/// Tasks plus the Identity user store. Uses <see cref="IdentityUserContext{TUser,TKey}"/> (no roles),
/// and drops the claim/login/token tables we don't use, leaving just Users and RefreshTokens.
/// </summary>
public class AppDbContext : IdentityUserContext<AppUser, int>
{
    private readonly TimeProvider _time;

    public AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider time) : base(options)
    {
        _time = time;
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<IdentityUserClaim<int>>();
        builder.Ignore<IdentityUserLogin<int>>();
        builder.Ignore<IdentityUserToken<int>>();

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Ignoring Identity's claim/login/token tables after the base class maps them is deliberate.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.MappedEntityTypeIgnoredWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All DateTimes are UTC. SQL Server's datetime2 has no kind, so mark values as UTC on the way
        // out; otherwise they serialize without a "Z" and browsers read them as local time.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HavePrecision(3);
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>().HavePrecision(3);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Current UTC time truncated to milliseconds, matching the datetime2(3) columns.</summary>
    public DateTime UtcNow()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    private void ApplyTimestamps()
    {
        var now = UtcNow();
        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.ModifiedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedDate = now;
                entry.Property(e => e.CreatedDate).IsModified = false;
            }
        }
    }
}
