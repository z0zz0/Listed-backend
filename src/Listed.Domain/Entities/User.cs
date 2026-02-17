using Listed.Domain.Exceptions;

namespace Listed.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordAlgorithm { get; private set; }
    public DateTime? PasswordUpdatedAt { get; private set; }
    public bool? IsVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsSoftDeleted { get; private set; }
    
    public UserInfo UserInfo { get; private set; } = null!;

    public ICollection<UserPhoto> Photos { get; private set; } = [];
    public ICollection<OrganisationMember> OrganisationMemberships { get; private set; } = [];
    public ICollection<EventParticipant> EventParticipations { get; private set; } = [];

    // EF Core
    private User() { }

    public User(string email, string passwordHash, string passwordAlgorithm)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UserDomainException("Email cannot be empty.");
        }

        if (!email.Contains('@'))
        {
            throw new UserDomainException("Invalid email format.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new UserDomainException("Password hash cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(passwordAlgorithm))
        {
            throw new UserDomainException("Password algorithm cannot be empty.");
        }

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        PasswordAlgorithm = passwordAlgorithm;
        PasswordUpdatedAt = DateTime.UtcNow;
        IsVerified = false;
        CreatedAt = DateTime.UtcNow;
        IsSoftDeleted = false;
    }

    public void SetUserInfo(UserInfo userInfo)
    {
        if (userInfo.Id != this.Id)
            throw new ArgumentException("UserInfo Id must match User Id.");

        UserInfo = userInfo;
    }
}
