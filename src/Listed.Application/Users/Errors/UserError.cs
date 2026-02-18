using Listed.Application.Common;

namespace Listed.Application.Users.Errors;

public class UserError
{
    public const string InvalidEmailCode = "User.Validation.InvalidEmail";
    public const string InvalidPasswordCode = "User.Validation.InvalidPassword";
    public const string PasswordTooShortCode = "User.Validation.PasswordTooShort";
    public const string InvalidUserDataCode = "User.Validation.InvalidUserData";
    public const string EmailAlreadyInUseCode = "User.Conflict.EmailAlreadyInUse";
    public const string UserNotFoundByIdCode = "User.NotFound.ById";
    public const string UserNotFoundByUserNameCode = "User.NotFound.ByUserName";

    public static Error InvalidEmail() =>
        new(InvalidEmailCode, "Invalid email address.");

    public static Error InvalidPassword() =>
        new(InvalidPasswordCode, "Invalid password.");

    public static Error InvalidPasswordTooShort(int minLength) =>
        new(PasswordTooShortCode, $"Password must be at least {minLength} characters long.");

    public static Error InvalidUserData(string details) =>
        new(InvalidUserDataCode, details);

    public static Error EmailAlreadyInUse(string email) =>
        new(EmailAlreadyInUseCode, $"Email '{email}' is already in use.");

    public static Error UserNotFoundById(Guid id) =>
        new(UserNotFoundByIdCode, $"User with ID '{id}' was not found.");

    public static Error UserNotFoundByUserName(string userName) =>
        new(UserNotFoundByUserNameCode, $"User with username '{userName}' was not found.");

    public static bool IsValidationCode(string code) =>
        code is InvalidEmailCode
            or InvalidPasswordCode
            or PasswordTooShortCode
            or InvalidUserDataCode;

    public static bool IsNotFoundCode(string code) =>
        code is UserNotFoundByIdCode
            or UserNotFoundByUserNameCode;
}
