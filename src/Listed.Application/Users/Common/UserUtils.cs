using Listed.Application.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Listed.Domain.Entities;

namespace Listed.Application.Users.Common;

public static class UserUtils
{
    public static Result<string> NormalizeAndValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<string>.Failure(UserError.InvalidEmail());
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!normalizedEmail.Contains('@'))
        {
            return Result<string>.Failure(UserError.InvalidEmail());
        }

        return Result<string>.Success(normalizedEmail);
    }

    public static GetUserResult MapToGetUserResult(this User user)
    {
        var userInfo = user.UserInfo?.MapToGetUserInfoResult();

        var photos = user.Photos
            .OrderBy(photo => photo.SortOrder)
            .Select(photo => new GetUserPhotoResult(photo.Id, photo.Url, photo.SortOrder, photo.UploadedAt))
            .ToArray();

        return new GetUserResult(
            user.Id,
            user.Email,
            user.IsVerified,
            user.IsSoftDeleted,
            userInfo,
            photos);
    }

    public static GetUserInfoResult MapToGetUserInfoResult(this UserInfo userInfo)
    {
        return new GetUserInfoResult(
            userInfo.Nationality,
            userInfo.FirstName,
            userInfo.LastName,
            userInfo.PhoneNumber,
            userInfo.HasPhonePrefix,
            userInfo.Biography);
    }
}
