using Listed.Application.Contracts.Signup;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Listed.Infrastructure.Signup;

public sealed class ValkeySignupVerificationStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<ValkeySignupVerificationStore> logger) : ISignupVerificationStore
{
    private const string SignupVerificationKeyPrefix = "signup:verification:id:";
    private static readonly JsonSerializerOptions SerializerOptions = new();
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<SignupVerificationState?> GetBySignupIdAsync(Guid signupId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(signupId);

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            var payload = value.ToString();
            return JsonSerializer.Deserialize<SignupVerificationState>(payload, SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while reading signup verification state. SignupId={SignupId}", signupId);
            throw;
        }
    }

    public async Task SetAsync(Guid signupId, SignupVerificationState state, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(signupId);

        try
        {
            if (ttl <= TimeSpan.Zero)
            {
                await _database.KeyDeleteAsync(key);
                return;
            }

            var payload = JsonSerializer.Serialize(state, SerializerOptions);
            await _database.StringSetAsync(key, payload, ttl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while saving signup verification state. SignupId={SignupId}", signupId);
            throw;
        }
    }

    private static string BuildKey(Guid signupId) => $"{SignupVerificationKeyPrefix}{signupId:N}";
}
