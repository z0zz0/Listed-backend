namespace Listed.Domain.Entities;

public abstract class Photo
{
    public Guid Id { get; private set; }
    public string Url { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }

    protected Photo() { }

    protected Photo(string url, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty.", nameof(url));

        Id = Guid.NewGuid();
        Url = url;
        SortOrder = sortOrder;
        UploadedAt = DateTime.UtcNow;
    }
}
