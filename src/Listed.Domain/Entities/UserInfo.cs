namespace Listed.Domain.Entities;

public class UserInfo
{
    public Guid Id { get; private set; }
    public string? Nationality { get; private set; }
    public string? NationalIdentificationNumber { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool? HasPhonePrefix { get; private set; }
    public string? Biography { get; private set; }

    public User User { get; private set; } = null!;
    
    // EF Core parameterless constructor
    private UserInfo() { }
    
    public UserInfo(
        Guid userId,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string? nationality = null,
        string? nationalIdNumber = null,
        string? phoneNumber = null,
        bool? hasPhonePrefix = null,
        string? biography = null
    )
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (!string.IsNullOrWhiteSpace(nationality) && nationality.Length != 2)
        {
            throw new ArgumentException("Nationality must be two-letter country code.", nameof(nationality));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber) && hasPhonePrefix is true)
        {
            throw new ArgumentException("Phone prefix cannot be set when phone number is missing.", nameof(hasPhonePrefix));
        }

        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Date of birth must be in the past.", nameof(dateOfBirth));
        }

        Id = userId;
        Nationality = string.IsNullOrWhiteSpace(nationality) ? null : nationality.Trim().ToUpperInvariant();
        NationalIdentificationNumber = string.IsNullOrWhiteSpace(nationalIdNumber) ? null : nationalIdNumber.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        HasPhonePrefix = hasPhonePrefix;
        Biography = biography?.Trim();
    }

    public void UpdateBiography(string? bio)
    {
        Biography = bio?.Trim();
    }
}
