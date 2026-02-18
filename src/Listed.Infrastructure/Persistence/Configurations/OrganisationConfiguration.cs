using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(o => o.CorporateIdentityNumber)    
            .IsRequired()
            .HasMaxLength(50);      
        
        builder.Property(o => o.Country)
            .IsRequired()
            .HasMaxLength(2);
        
        builder.Property(o => o.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(o => new { o.Country, o.CorporateIdentityNumber })
            .IsUnique()
            .HasDatabaseName(PersistenceConstraintNames.Organisation.CountryCinUnique);

        // Relationships
        builder.HasMany(o => o.Members)
            .WithOne(m => m.Organisation)
            .HasForeignKey(m => m.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Photos)
            .WithOne(p => p.Organisation)
            .HasForeignKey(p => p.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Events)
            .WithOne(e => e.Organisation)
            .HasForeignKey(e => e.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
