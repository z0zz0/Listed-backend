using Listed.Application.Common;

namespace Listed.Application.Auth.Errors;

public static class AuthError
{
    public const string InvalidEmailCode = "Auth.Validation.InvalidEmail";
    public const string InvalidPasswordCode = "Auth.Validation.InvalidPassword";
    public const string MissingRefreshTokenCode = "Auth.Validation.MissingRefreshToken";
    public const string InvalidCredentialsCode = "Auth.Unauthorized.InvalidCredentials";
    public const string InvalidRefreshTokenCode = "Auth.Unauthorized.InvalidRefreshToken";
    public const string ExpiredRefreshTokenCode = "Auth.Unauthorized.ExpiredRefreshToken";
    public const string ReusedRefreshTokenCode = "Auth.Unauthorized.ReusedRefreshToken";
    public const string AlreadyLoggedInOnThisDeviceCode = "Auth.Conflict.AlreadyLoggedInOnThisDevice";
    public const string UserNotFoundCode = "Auth.NotFound.User";
    public const string TokenGenerationFailedCode = "Auth.Internal.TokenGenerationFailed";

    public static Error InvalidEmail() =>
        new(InvalidEmailCode, "Invalid email address.");

    public static Error InvalidPassword() =>
        new(InvalidPasswordCode, "Invalid password.");

    public static Error MissingRefreshToken() =>
        new(MissingRefreshTokenCode, "Refresh token is missing.");

    public static Error InvalidCredentials() =>
        new(InvalidCredentialsCode, "Invalid email or password.");

    public static Error InvalidRefreshToken() =>
        new(InvalidRefreshTokenCode, "Invalid refresh token.");

    public static Error ExpiredRefreshToken() =>
        new(ExpiredRefreshTokenCode, "Refresh token has expired.");

    public static Error ReusedRefreshToken() =>
        new(ReusedRefreshTokenCode, "Refresh token was already used.");

    public static Error AlreadyLoggedInOnThisDevice() =>
        new(AlreadyLoggedInOnThisDeviceCode, "An active session already exists on this device.");

    public static Error UserNotFound(Guid userId) =>
        new(UserNotFoundCode, $"User with id '{userId}' was not found.");

    public static Error TokenGenerationFailed() =>
        new(TokenGenerationFailedCode, "Could not generate a unique refresh token.");

    public static bool IsValidationCode(string code) =>
        code is InvalidEmailCode
            or InvalidPasswordCode
            or MissingRefreshTokenCode;

    public static bool IsUnauthorizedCode(string code) =>
        code is InvalidCredentialsCode
            or InvalidRefreshTokenCode
            or ExpiredRefreshTokenCode
            or ReusedRefreshTokenCode;

    public static bool IsConflictCode(string code) =>
        code is AlreadyLoggedInOnThisDeviceCode;
}
