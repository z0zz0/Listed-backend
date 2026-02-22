using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordAlgorithm)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(u => u.PasswordUpdatedAt)
            .IsRequired(false);

        builder.Property(u => u.IsVerified)
            .IsRequired(false);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsSoftDeleted)
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName(PersistenceConstraintNames.User.EmailUnique);

        // Relationships
        builder.HasOne(u => u.UserInfo)
               .WithOne(ui => ui.User)
               .HasForeignKey<UserInfo>(ui => ui.Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.AuthInfo)
               .WithOne(ai => ai.User)
               .HasForeignKey<AuthInfo>(ai => ai.Id)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.Photos)
               .WithOne(p => p.User)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OrganisationMemberships)
               .WithOne(m => m.User)
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.EventParticipations)
               .WithOne(ep => ep.User)
               .HasForeignKey(ep => ep.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

