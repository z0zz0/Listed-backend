using Listed.Domain.Exceptions;

namespace Listed.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Nationality { get; private set; }
    public string NationalIdentificationNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool HasPhonePrefix { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordAlgorithm { get; private set; }
    public DateTime? PasswordUpdatedAt { get; private set; }
    public string? Biography { get; private set; }
    public bool? IsVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsSoftDeleted { get; private set; }

    public ICollection<UserPhoto> Photos { get; private set; } = [];
    public ICollection<OrganisationMember> OrganisationMemberships { get; private set; } = [];
    public ICollection<EventParticipant> EventParticipations { get; private set; } = [];

    private User() { } // EF Core

    public User(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UserDomainException("Email cannot be empty.");
        }

        if (!email.Contains('@')) {
            throw new UserDomainException("Invalid email format.");
        }

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateBio(string newBiography)
    {
        if (newBiography?.Length > 500)
        {
            throw new UserDomainException("Bio cannot exceed 500 characters.");
        }

        Biography = newBiography?.Trim() ?? string.Empty;
    }
}
