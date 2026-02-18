using Listed.Application.Contracts.Persistence;
using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Listed.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(ListedDbContext dbContext) : IUserRepository
{
    private static readonly IReadOnlyDictionary<string, string> UniqueConstraintCodeByName =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PersistenceConstraintNames.User.EmailUnique] = PersistenceConstraintCodes.User.EmailUnique
        };

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresEx
                                           && postgresEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var constraintName = postgresEx.ConstraintName;
            var constraintCode = constraintName is not null && UniqueConstraintCodeByName.TryGetValue(constraintName, out var code)
                ? code
                : PersistenceConstraintCodes.Common.UnknownUnique;

            throw new UniqueConstraintViolationException(constraintCode, constraintName);
        }
    }
}
