namespace Listed.Application.Contracts.Signup;

public interface ISignupVerificationStore
{
    Task<SignupVerificationState?> GetBySignupIdAsync(Guid signupId, CancellationToken cancellationToken);
    Task SetAsync(Guid signupId, SignupVerificationState state, TimeSpan ttl, CancellationToken cancellationToken);
}
