using Listed.Application.Common;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Errors;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Listed.Application.Users.Common;

public static class SignupUtils
{
    public const int VerificationCodeLength = 6;
    public const int MaxVerificationAttempts = 5;
    public static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan VerifiedStateLifetime = TimeSpan.FromHours(24);

    public static Result<string> NormalizeAndValidateVerificationCode(string? verificationCode)
    {
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            return Result<string>.Failure(UserError.InvalidVerificationCode());
        }

        var normalizedCode = verificationCode.Trim();
        if (normalizedCode.Length != VerificationCodeLength || !normalizedCode.All(char.IsDigit))
        {
            return Result<string>.Failure(UserError.InvalidVerificationCode());
        }

        return Result<string>.Success(normalizedCode);
    }

    public static string GenerateVerificationCode()
    {
        Span<byte> randomBytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(randomBytes);

        var codeValue = BitConverter.ToUInt32(randomBytes) % 1_000_000;
        return codeValue.ToString($"D{VerificationCodeLength}", CultureInfo.InvariantCulture);
    }

    public static string HashVerificationCode(string verificationCode)
    {
        var codeBytes = Encoding.UTF8.GetBytes(verificationCode);
        var hashBytes = SHA256.HashData(codeBytes);
        return Convert.ToHexString(hashBytes);
    }

    public static bool DoesVerificationCodeHashMatch(string verificationCode, string expectedCodeHash)
    {
        var providedHashBytes = Convert.FromHexString(HashVerificationCode(verificationCode));
        var expectedHashBytes = Convert.FromHexString(expectedCodeHash);

        return CryptographicOperations.FixedTimeEquals(providedHashBytes, expectedHashBytes);
    }

    public static Result<SignupVerificationState> ValidateVerifiedSignupState(SignupVerificationState? signupState, DateTime now)
    {
        if (signupState is null)
        {
            return Result<SignupVerificationState>.Failure(UserError.SignupStateMissing());
        }

        if (!signupState.IsVerified)
        {
            return Result<SignupVerificationState>.Failure(UserError.SignupEmailNotVerified());
        }

        if (signupState.ExpiresAtUtc <= now)
        {
            return Result<SignupVerificationState>.Failure(UserError.VerificationCodeExpired());
        }

        return Result<SignupVerificationState>.Success(signupState);
    }
}
