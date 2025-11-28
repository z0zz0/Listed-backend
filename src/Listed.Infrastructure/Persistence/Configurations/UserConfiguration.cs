using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nationality)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(u => u.NationalIdentificationNumber)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.HasPhonePrefix)
            .IsRequired();

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

        builder.Property(u => u.Biography)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(u => u.IsVerified)
            .IsRequired(false);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsSoftDeleted)
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.NationalIdentificationNumber)
            .IsUnique()
            .HasDatabaseName("unique_index_users_nin");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("unique_index_users_email");

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("unique_index_users_phone_number");
        
        // Relationships
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
