using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.OrganisationId)
            .IsRequired();
        
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.LowerAgeLimit)
            .IsRequired();
        
        builder.Property(e => e.UpperAgeLimit)
            .IsRequired(false);
        
        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired(false);
        
        builder.Property(e => e.StartTime)
            .IsRequired();
        
        builder.Property(e => e.EndTime)
            .IsRequired(false);
        
        builder.Property(builder => builder.CreatedBy)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .IsRequired(false);

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.Status)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.OrganisationId)
            .HasDatabaseName(PersistenceConstraintNames.Event.OrganisationIdIndex);

        // Relationships
        builder.HasMany(e => e.Participants)
            .WithOne(ep => ep.Event)
            .HasForeignKey(ep => ep.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Photos)
            .WithOne(ep => ep.Event)
            .HasForeignKey(ep => ep.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
