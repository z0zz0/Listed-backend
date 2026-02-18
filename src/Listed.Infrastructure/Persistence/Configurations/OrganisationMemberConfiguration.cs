using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations;

public class OrganisationMemberConfiguration : IEntityTypeConfiguration<OrganisationMember>
{
    public void Configure(EntityTypeBuilder<OrganisationMember> builder)
    {
        builder.HasKey(om => om.Id);

        builder.Property(om => om.OrganisationId)
            .IsRequired();

        builder.Property(om => om.UserId)
            .IsRequired();

        builder.Property(om => om.Role)
            .IsRequired();

        builder.Property(om => om.JoinedAt)
            .IsRequired();

        builder.Property(om => om.LeftAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(om => new { om.OrganisationId, om.UserId})
            .IsUnique()
            .HasDatabaseName(PersistenceConstraintNames.OrganisationMember.OrganisationUserUnique);
    }
}
