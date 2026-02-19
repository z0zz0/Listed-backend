namespace Listed.Application.Users.Results;

public sealed record GetUserPhotoResult(
    Guid Id,
    string Url,
    int SortOrder,
    DateTime UploadedAt);
