using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.Persistence;

public class ListedDbContext(DbContextOptions<ListedDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ListedDbContext).Assembly);
    }
}