using Listed.Application.Common;
using Listed.Application.Users.Errors;

namespace Listed.Application.Users.Common;

public static class UserUtils
{
    public const int MinPasswordLength = 8;

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

    public static Result<string> NormalizeAndValidateFirstName(string? firstName)
    {
        return NormalizeAndValidateName(firstName, UserError.InvalidFirstName);
    }

    public static Result<string> NormalizeAndValidateLastName(string? lastName)
    {
        return NormalizeAndValidateName(lastName, UserError.InvalidLastName);
    }

    public static Result<DateOnly> ValidateDateOfBirth(DateOnly dateOfBirth)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth >= todayUtc)
        {
            return Result<DateOnly>.Failure(UserError.InvalidDateOfBirth());
        }

        return Result<DateOnly>.Success(dateOfBirth);
    }

    public static Result<string> ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result<string>.Failure(UserError.InvalidPassword());
        }

        if (password.Length < MinPasswordLength)
        {
            return Result<string>.Failure(UserError.InvalidPasswordTooShort(MinPasswordLength));
        }

        return Result<string>.Success(password);
    }

    private static Result<string> NormalizeAndValidateName(string? name, Func<Error> errorFactory)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<string>.Failure(errorFactory());
        }

        var normalizedName = name.Trim();

        if (normalizedName.Contains(' ')
            || normalizedName.StartsWith('-')
            || normalizedName.EndsWith('-')
            || normalizedName.Contains("--"))
        {
            return Result<string>.Failure(errorFactory());
        }

        if (!normalizedName.All(c => char.IsLetter(c) || c == '-'))
        {
            return Result<string>.Failure(errorFactory());
        }

        var normalizedChars = normalizedName.ToLowerInvariant().ToCharArray();
        var shouldUppercase = true;

        for (var i = 0; i < normalizedChars.Length; i++)
        {
            if (normalizedChars[i] == '-')
            {
                shouldUppercase = true;
                continue;
            }

            if (shouldUppercase)
            {
                normalizedChars[i] = char.ToUpperInvariant(normalizedChars[i]);
                shouldUppercase = false;
            }
        }

        return Result<string>.Success(new string(normalizedChars));
    }
}
