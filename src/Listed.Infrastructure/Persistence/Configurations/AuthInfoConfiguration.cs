using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class AuthInfoConfiguration : IEntityTypeConfiguration<AuthInfo>
{
    public void Configure(EntityTypeBuilder<AuthInfo> builder)
    {
        builder.HasKey(ai => ai.Id);

        builder.Property(ai => ai.AuthVersion)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(ai => ai.User)
            .WithOne(u => u.AuthInfo)
            .HasForeignKey<AuthInfo>(ai => ai.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ai => ai.RefreshTokens)
            .WithOne(rt => rt.AuthInfo)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
