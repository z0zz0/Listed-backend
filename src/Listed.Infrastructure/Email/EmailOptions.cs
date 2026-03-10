namespace Listed.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool UseTls { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Listed";

    public bool ShouldUseAuthentication => !string.IsNullOrWhiteSpace(Username);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException("Email:Host is missing.");
        }

        if (Port <= 0)
        {
            throw new InvalidOperationException("Email:Port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new InvalidOperationException("Email:FromAddress is missing.");
        }

        if (ShouldUseAuthentication && string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("Email:Password is required when Email:Username is set.");
        }

        if (!ShouldUseAuthentication && !string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("Email:Username is required when Email:Password is set.");
        }
    }
}
