using Listed.Domain.Entities;
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
            .HasDatabaseName("index_event_photos_event_id");
    }
}
