using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listed.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Primary key
            builder.HasKey(u => u.Id);

            // Map the private '_id' field to the public 'Id' property
            builder.Property(u => u.Id)
                .HasField("_id")        // Tells EF Core to use the '_id' backing field
                .ValueGeneratedNever(); // Since the GUID is generated in the domain constructor

            // Enforce required fields and length constraints
            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.Bio)
                .HasMaxLength(500);  // Optional: limit length for practical purposes

            // For CreatedAt, you might keep it just as is; typically no special config needed
            // but you could specify a default value or custom column name if you want:
            // builder.Property(u => u.CreatedAt)
            //       .HasColumnName("CreatedAt")
            //       .HasDefaultValueSql("NOW()");

            // Optional: Add an index (like a unique index on Email if your domain requires it)
            // builder.HasIndex(u => u.Email).IsUnique();
        }
    }
}
