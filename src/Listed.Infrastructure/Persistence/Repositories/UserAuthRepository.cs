using Listed.Application.Contracts.Persistence;
using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.Persistence.Repositories;

public sealed class UserAuthRepository(ListedDbContext dbContext) : IUserAuthRepository
{
    public Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(u => u.AuthInfo)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdForAuthAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(u => u.AuthInfo)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> IncrementAuthVersionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.AuthInfos
            .Where(ai => ai.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ai => ai.AuthVersion, ai => ai.AuthVersion + 1), cancellationToken);

        return affectedRows > 0;
    }
}
