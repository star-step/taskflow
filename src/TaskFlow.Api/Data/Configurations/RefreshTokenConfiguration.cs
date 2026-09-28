using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Data.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength().IsUnicode(false);
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        b.Property(x => x.SecurityStamp).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.FamilyId);

        b.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
