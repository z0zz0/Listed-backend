using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class EventPhotoConfiguration : IEntityTypeConfiguration<EventPhoto>
{
    public void Configure(EntityTypeBuilder<EventPhoto> builder)
    {
        builder.HasKey(ep => ep.Id);
        
        builder.Property(ep => ep.EventId)
            .IsRequired();
        
        builder.Property(ep => ep.Url)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(ep => ep.SortOrder)
            .IsRequired();
        
        builder.Property(ep => ep.UploadedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ep => ep.EventId)
            .HasDatabaseName(PersistenceConstraintNames.EventPhoto.EventIdIndex);
    }
}
