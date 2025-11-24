using Listed.Domain.Enums;

namespace Listed.Domain.Entities;

public class OrganisationMember
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrganisationRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Organisation Organisation { get; private set; }
    public User User { get; private set; }

    private OrganisationMember() { }
}