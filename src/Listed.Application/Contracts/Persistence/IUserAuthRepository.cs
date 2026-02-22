using Listed.Domain.Entities;

namespace Listed.Application.Contracts.Persistence;

public interface IUserAuthRepository
{
    Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdForAuthAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IncrementAuthVersionAsync(Guid userId, CancellationToken cancellationToken);
}
