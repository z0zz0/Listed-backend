namespace Listed.Domain.Entities
{
    public class User
    {
        // Backing field to ensure the ID is never settable externally
        private readonly Guid _id;

        // Public read-only property that exposes the GUID
        public Guid Id => _id;

        // Basic profile info
        public string UserName { get; private set; }
        public string Email { get; private set; }
        public string Bio { get; private set; } // "About Me" section

        public DateTime CreatedAt { get; private set; }

        private User() { /* Required for EF Core or serialization */ }

        public User(string userName, string email, string bio)
        {
            // Generate a new GUID upon creation
            _id = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Username is required", nameof(userName));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            UserName = userName;
            Email = email;
            Bio = bio;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateBio(string newBio)
        {
            // Possibly validate length or content
            Bio = newBio;
        }
    }
}
