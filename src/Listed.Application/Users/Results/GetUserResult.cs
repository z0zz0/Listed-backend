namespace Listed.Application.Users.Results;

public sealed record GetUserResult(
    Guid Id,
    string Email,
    bool? IsVerified,
    bool IsSoftDeleted,
    GetUserInfoResult? UserInfo,
    IReadOnlyCollection<GetUserPhotoResult> Photos);
