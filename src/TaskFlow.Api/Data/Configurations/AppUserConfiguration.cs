using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Api.Domain;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Api.Data.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.Property(x => x.DisplayName).IsRequired().HasMaxLength(AccountRules.DisplayNameMaxLength);
        b.Property(x => x.Email).HasMaxLength(AccountRules.EmailMaxLength);

        // Identity indexes NormalizedEmail but doesn't make it unique; we rely on uniqueness.
        b.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}
