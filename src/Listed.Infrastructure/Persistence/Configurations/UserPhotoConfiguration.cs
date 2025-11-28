using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class UserPhotoConfiguration : IEntityTypeConfiguration<UserPhoto>
{
    public void Configure(EntityTypeBuilder<UserPhoto> builder)
    {
        builder.HasKey(up => up.Id);
        
        builder.Property(up => up.UserId)
            .IsRequired();

        builder.Property(up => up.Url)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(up => up.SortOrder)
            .IsRequired();

        builder.Property(up => up.UploadedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("index_user_photos_user_id");
    }
}
