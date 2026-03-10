using Listed.API.Contracts.Auth;

namespace Listed.API.Contracts.Users;

public sealed record CompleteSignupResponse(Guid Id, string Email, AccessTokenResponse AccessToken);
