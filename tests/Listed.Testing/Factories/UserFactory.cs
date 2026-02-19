using Listed.Domain.Entities;
using System.Reflection;

namespace Listed.Testing.Factories;

public static class UserFactory
{
    public static User Valid(
        string? email = null,
        string? passwordHash = null,
        string? algorithm = null,
        bool includeUserInfo = false,
        int photoCount = 0)
    {
        var user = new User(
            email ?? "user@test.io",
            passwordHash ?? "hashed-password",
            algorithm ?? "bcrypt");

        if (includeUserInfo)
        {
            var userInfo = UserInfoFactory.Valid();
            SetPrivateProperty(userInfo, nameof(UserInfo.Id), user.Id);
            user.SetUserInfo(userInfo);
        }

        for (var i = 1; i <= photoCount; i++)
        {
            user.Photos.Add(UserPhotoFactory.Valid(user.Id, sortOrder: i));
        }

        return user;
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
}
