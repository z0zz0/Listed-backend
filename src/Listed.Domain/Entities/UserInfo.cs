namespace Listed.Domain.Entities;

public class UserInfo
{
    public Guid Id { get; private set; }
    public string Nationality { get; private set; }
    public string NationalIdentificationNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool HasPhonePrefix { get; private set; }
    public string? Biography { get; private set; }

    public User User { get; private set; } = null!;
    
    // EF Core parameterless constructor
    private UserInfo() { }
    
    public UserInfo(
        string nationality,
        string nationalIdNumber,
        string firstName,
        string lastName,
        string phoneNumber,
        bool hasPhonePrefix,
        string? biography = null
    )
    {
        if (string.IsNullOrWhiteSpace(nationality))
        { 
            throw new ArgumentException("Nationality is required.", nameof(nationality));
        }

        if (nationality.Length != 2) {
            throw new ArgumentException("Nationality must be two-letter country code.", nameof(nationality));
        }

        if (string.IsNullOrWhiteSpace(nationalIdNumber))
        {
            throw new ArgumentException("National ID number is required.", nameof(nationalIdNumber));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        Nationality = nationality;
        NationalIdentificationNumber = nationalIdNumber;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        HasPhonePrefix = hasPhonePrefix;
        Biography = biography?.Trim();
    }

    public void UpdateBiography(string? bio)
    {
        Biography = bio?.Trim();
    }
}
