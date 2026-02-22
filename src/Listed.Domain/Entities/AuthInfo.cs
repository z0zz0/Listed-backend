namespace Listed.Domain.Entities;

public class AuthInfo
{
    public Guid Id { get; private set; }
    public int AuthVersion { get; private set; }

    public User User { get; private set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private AuthInfo() { }

    public AuthInfo(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        Id = userId;
        AuthVersion = 0;
    }
}
