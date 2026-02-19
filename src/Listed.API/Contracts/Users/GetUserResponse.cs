namespace Listed.API.Contracts.Users;

public sealed record GetUserResponse(
    Guid Id,
    string Email,
    bool? IsVerified,
    bool IsSoftDeleted,
    GetUserInfoResponse? UserInfo,
    IReadOnlyCollection<GetUserPhotoResponse> Photos);
