using Listed.Domain.Enums;

namespace Listed.Domain.Entities;

public class EventParticipant
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public ParticipationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Event Event { get; private set; }
    public User User { get; private set; }

    private EventParticipant() { }
}
