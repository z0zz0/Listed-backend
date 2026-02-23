using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rt => rt.DeviceId)
            .IsRequired();

        builder.Property(rt => rt.SessionId)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .IsRequired(false);

        builder.Property(rt => rt.ReplacedByTokenId)
            .IsRequired(false);

        builder.Property(rt => rt.CreatedByIp)
            .IsRequired(false)
            .HasMaxLength(64);

        builder.Property(rt => rt.CreatedByUserAgent)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName(PersistenceConstraintNames.RefreshToken.TokenHashUnique);

        builder.HasIndex(rt => new { rt.UserId, rt.RevokedAt })
            .HasDatabaseName(PersistenceConstraintNames.RefreshToken.UserActiveLookup);

        builder.HasIndex(rt => new { rt.UserId, rt.DeviceId })
            .IsUnique()
            .HasFilter("\"revoked_at\" IS NULL")
            .HasDatabaseName(PersistenceConstraintNames.RefreshToken.UserDeviceActiveUnique);

        builder.HasIndex(rt => rt.SessionId)
            .IsUnique()
            .HasFilter("\"revoked_at\" IS NULL")
            .HasDatabaseName(PersistenceConstraintNames.RefreshToken.SessionActiveUnique);
    }
}
