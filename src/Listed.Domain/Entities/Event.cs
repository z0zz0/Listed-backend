using Listed.Domain.Enums;

namespace Listed.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string Title { get; private set; } = null!;
    public int LowerAgeLimit { get; private set; }
    public int? UpperAgeLimit { get; private set; }
    public string Location { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public EventStatus Status { get; private set; }

    public Organisation Organisation { get; private set; } = null!;
    public ICollection<EventParticipant> Participants { get; private set; } = [];
    public ICollection<EventPhoto> Photos { get; private set; } = [];

    private Event() { }
}
