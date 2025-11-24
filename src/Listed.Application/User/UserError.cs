using Listed.Application.Common;

namespace Listed.Application.User;

public class UserError
{
    public static Error UserNotFoundById(Guid id) =>
        new("User.Error.UserNotFoundById", $"User with ID '{id}' was not found.");

    public static Error UserNotFoundByUserName(string userName) =>
        new("User.Error.UserNotFoundByUserName", $"User with username '{userName}' was not found.");
}
