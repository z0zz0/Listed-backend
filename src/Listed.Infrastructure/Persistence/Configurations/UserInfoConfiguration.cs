using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class UserInfoConfiguration : IEntityTypeConfiguration<UserInfo>
{
    public void Configure(EntityTypeBuilder<UserInfo> builder)
    {
        builder.HasKey(ui => ui.Id);
        
        builder.Property(ui => ui.Nationality)
            .IsRequired(false)
            .HasMaxLength(2);

        builder.Property(ui => ui.NationalIdentificationNumber)
            .IsRequired(false)
            .HasMaxLength(25);

        builder.Property(ui => ui.FirstName)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(ui => ui.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ui => ui.DateOfBirth)
            .IsRequired();

        builder.Property(ui => ui.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(20);

        builder.Property(ui => ui.HasPhonePrefix)
            .IsRequired(false);


        builder.Property(ui => ui.Biography)
            .IsRequired(false)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(ui => ui.NationalIdentificationNumber)
            .IsUnique()
            .HasDatabaseName(PersistenceConstraintNames.UserInfo.NinUnique);

        builder.HasIndex(ui => ui.PhoneNumber)
            .IsUnique()
            .HasDatabaseName(PersistenceConstraintNames.UserInfo.PhoneNumberUnique);
    }
}
