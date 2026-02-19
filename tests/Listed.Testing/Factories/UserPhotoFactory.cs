using Listed.Domain.Entities;

namespace Listed.Testing.Factories;

public static class UserPhotoFactory
{
    public static UserPhoto Valid(Guid userId, string? url = null, int sortOrder = 1)
    {
        return new UserPhoto(
            userId,
            url ?? $"https://cdn.test/user-{sortOrder}.jpg",
            sortOrder);
    }
}
