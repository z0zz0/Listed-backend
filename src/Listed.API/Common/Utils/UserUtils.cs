using Listed.API.Contracts.Users;
using Listed.Application.Users.Results;

namespace Listed.API.Common.Utils;

public static class UserUtils
{
    public static GetUserResponse MapToGetUserResponse(this GetUserResult user)
    {
        var userInfo = user.UserInfo?.MapToGetUserInfoResponse();

        var photos = user.Photos
            .Select(photo => new GetUserPhotoResponse(photo.Id, photo.Url, photo.SortOrder, photo.UploadedAt))
            .ToArray();

        return new GetUserResponse(
            user.Id,
            user.Email,
            user.IsVerified,
            user.IsSoftDeleted,
            userInfo,
            photos);
    }

    public static GetUserInfoResponse MapToGetUserInfoResponse(this GetUserInfoResult userInfo)
    {
        return new GetUserInfoResponse(
            userInfo.Nationality,
            userInfo.FirstName,
            userInfo.LastName,
            userInfo.PhoneNumber,
            userInfo.HasPhonePrefix,
            userInfo.Biography);
    }
}
