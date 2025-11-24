namespace Listed.Domain.Entities;

public class EventPhoto : Photo
{
    public Guid EventId { get; private set; }
    public Event Event { get; private set; }

    private EventPhoto() { }

    public EventPhoto(Guid eventId, string url, int sortOrder) : base(url, sortOrder)
    {
        EventId = eventId;
    }
}
