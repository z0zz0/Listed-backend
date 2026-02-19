namespace Listed.API.Contracts.Users;

public sealed record GetUserPhotoResponse(
    Guid Id,
    string Url,
    int SortOrder,
    DateTime UploadedAt);
