namespace Listed.Domain.Entities;

public class UserPhoto : Photo
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    private UserPhoto() { }

    public UserPhoto(Guid userId, string url, int sortOrder) : base(url, sortOrder)
    {
        UserId = userId;
    }
}
