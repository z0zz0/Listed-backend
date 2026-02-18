using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class OrganisationPhotoConfiguration : IEntityTypeConfiguration<OrganisationPhoto>
{
    public void Configure(EntityTypeBuilder<OrganisationPhoto> builder)
    {
        builder.HasKey(op => op.Id);

        builder.Property(op => op.OrganisationId)
            .IsRequired();

        builder.Property(op => op.Url)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(op => op.SortOrder)
            .IsRequired();

        builder.Property(op => op.UploadedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(op => op.OrganisationId)
            .HasDatabaseName(PersistenceConstraintNames.OrganisationPhoto.OrganisationIdIndex);
    }
}
