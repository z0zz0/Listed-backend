using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.EventId)
            .IsRequired();
        
        builder.Property(ep => ep.UserId)
            .IsRequired();

        builder.Property(ep => ep.Status)
            .IsRequired();
        
        builder.Property(ep => ep.CreatedAt)
            .IsRequired();

        builder.Property(ep => ep.UpdatedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(ep => ep.EventId)
            .HasDatabaseName("index_event_participants_event_id");
        
        builder.HasIndex(ep => new {ep.EventId, ep.UserId})
            .IsUnique()
            .HasDatabaseName("unique_index_event_participants_event_id_user_id");
    }
}
