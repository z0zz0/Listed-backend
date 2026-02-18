namespace Listed.Domain.Entities;

public class OrganisationPhoto : Photo
{
    public Guid OrganisationId { get; private set; }
    public Organisation Organisation { get; private set; } = null!;
    
    private OrganisationPhoto() { }
    
    public OrganisationPhoto(Guid organisationId, string url, int sortOrder) : base(url, sortOrder)
    {
        OrganisationId = organisationId;
    }
}
