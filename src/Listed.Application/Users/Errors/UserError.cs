using Listed.Application.Common;

namespace Listed.Application.Users.Errors;

public class UserError
{
    public const string InvalidEmailCode = "User.Validation.InvalidEmail";
    public const string InvalidPasswordCode = "User.Validation.InvalidPassword";
    public const string PasswordTooShortCode = "User.Validation.PasswordTooShort";
    public const string InvalidFirstNameCode = "User.Validation.InvalidFirstName";
    public const string InvalidLastNameCode = "User.Validation.InvalidLastName";
    public const string InvalidDateOfBirthCode = "User.Validation.InvalidDateOfBirth";
    public const string InvalidVerificationCodeCode = "User.Validation.InvalidVerificationCode";
    public const string VerificationCodeExpiredCode = "User.Validation.VerificationCodeExpired";
    public const string SignupStateMissingCode = "User.Validation.SignupStateMissing";
    public const string SignupEmailNotVerifiedCode = "User.Validation.SignupEmailNotVerified";
    public const string SignupPersonalInfoIncompleteCode = "User.Validation.SignupPersonalInfoIncomplete";
    public const string InvalidUserDataCode = "User.Validation.InvalidUserData";
    public const string VerificationAttemptsExceededCode = "User.RateLimit.VerificationAttemptsExceeded";
    public const string EmailAlreadyInUseCode = "User.Conflict.EmailAlreadyInUse";
    public const string SignupEmailDeliveryFailedCode = "User.Internal.SignupEmailDeliveryFailed";

    public static Error InvalidEmail() =>
        new(InvalidEmailCode, "Invalid email address.");

    public static Error InvalidPassword() =>
        new(InvalidPasswordCode, "Invalid password.");

    public static Error InvalidPasswordTooShort(int minLength) =>
        new(PasswordTooShortCode, $"Password must be at least {minLength} characters long.");

    public static Error InvalidFirstName() =>
        new(InvalidFirstNameCode, "First name is invalid.");

    public static Error InvalidLastName() =>
        new(InvalidLastNameCode, "Last name is invalid.");

    public static Error InvalidDateOfBirth() =>
        new(InvalidDateOfBirthCode, "Date of birth is invalid.");

    public static Error InvalidVerificationCode() =>
        new(InvalidVerificationCodeCode, "Verification code is invalid.");

    public static Error VerificationCodeExpired() =>
        new(VerificationCodeExpiredCode, "Verification code has expired.");

    public static Error SignupStateMissing() =>
        new(SignupStateMissingCode, "Signup state was not found. Start signup again.");

    public static Error SignupEmailNotVerified() =>
        new(SignupEmailNotVerifiedCode, "Email must be verified before continuing signup.");

    public static Error SignupPersonalInfoIncomplete() =>
        new(SignupPersonalInfoIncompleteCode, "Signup personal info is incomplete.");

    public static Error InvalidUserData(string details) =>
        new(InvalidUserDataCode, details);

    public static Error VerificationAttemptsExceeded(int maxAttempts) =>
        new(VerificationAttemptsExceededCode, $"Verification attempts exceeded. Maximum allowed attempts: {maxAttempts}.");

    public static Error EmailAlreadyInUse(string email) =>
        new(EmailAlreadyInUseCode, $"Email '{email}' is already in use.");

    public static Error SignupEmailDeliveryFailed() =>
        new(SignupEmailDeliveryFailedCode, "Could not send the verification email.");

    public static bool IsValidationCode(string code) =>
        code is InvalidEmailCode
            or InvalidPasswordCode
            or PasswordTooShortCode
            or InvalidFirstNameCode
            or InvalidLastNameCode
            or InvalidDateOfBirthCode
            or InvalidVerificationCodeCode
            or VerificationCodeExpiredCode
            or SignupStateMissingCode
            or SignupEmailNotVerifiedCode
            or SignupPersonalInfoIncompleteCode
            or InvalidUserDataCode;

    public static bool IsRateLimitCode(string code) =>
        code is VerificationAttemptsExceededCode;

    public static bool IsInternalCode(string code) =>
        code is SignupEmailDeliveryFailedCode;
}
