using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.Persistence;

public class ListedDbContext(DbContextOptions<ListedDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserInfo> PersonInfos => Set<UserInfo>();
    public DbSet<UserPhoto> UserPhotos => Set<UserPhoto>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<OrganisationMember> OrganisationMembers => Set<OrganisationMember>();
    public DbSet<OrganisationPhoto> OrganisationPhotos => Set<OrganisationPhoto>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<EventPhoto> EventPhotos => Set<EventPhoto>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ListedDbContext).Assembly);
    }
}