namespace Listed.Domain.Entities;

public class Organisation
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string CorporateIdentityNumber { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public ICollection<OrganisationMember> Members { get; private set; } = [];
    public ICollection<OrganisationPhoto> Photos { get; private set; } = [];
    public ICollection<Event> Events { get; private set; } = [];

    private Organisation() { }
}
