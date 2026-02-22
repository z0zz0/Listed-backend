using Listed.Application.Contracts.Persistence;
using Listed.Domain.Entities;
using Listed.Infrastructure.Persistence;
using Listed.Infrastructure.Persistence.Repositories;
using Listed.Testing.Factories;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class RefreshTokenRepositoryTests : IClassFixture<InfrastructureDatabaseFixture>, IAsyncLifetime
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public RefreshTokenRepositoryTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_AndGetByHashAsync_WhenValid_PersistsAndReturnsToken()
    {
        var user = await SeedUserAsync("refresh.add@get.io");

        await using var writeContext = _fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(writeContext);
        var refreshToken = CreateRefreshToken(user.Id, "token-hash-1");

        await repository.AddAsync(refreshToken, CancellationToken.None);

        await using var readContext = _fixture.CreateDbContext();
        var readRepository = new RefreshTokenRepository(readContext);
        var found = await readRepository.GetByHashAsync("token-hash-1", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(refreshToken.Id, found!.Id);
        Assert.Equal(user.Id, found.UserId);
    }

    [Fact]
    public async Task RotateAsync_WhenCurrentTokenActive_RevokesCurrentAndCreatesReplacement()
    {
        var user = await SeedUserAsync("refresh.rotate@get.io");
        var current = CreateRefreshToken(user.Id, "token-hash-current");
        var replacement = CreateRefreshToken(user.Id, "token-hash-next");

        await using (var seedContext = _fixture.CreateDbContext())
        {
            await seedContext.RefreshTokens.AddAsync(current);
            await seedContext.SaveChangesAsync();
        }

        await using var writeContext = _fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(writeContext);
        var now = DateTime.UtcNow;

        var rotated = await repository.RotateAsync(current.Id, replacement, now, CancellationToken.None);

        Assert.True(rotated);

        await using var readContext = _fixture.CreateDbContext();
        var currentFromDb = await readContext.RefreshTokens.AsNoTracking().SingleAsync(rt => rt.Id == current.Id);
        var replacementFromDb = await readContext.RefreshTokens.AsNoTracking().SingleAsync(rt => rt.Id == replacement.Id);

        Assert.NotNull(currentFromDb.RevokedAt);
        Assert.Equal(replacement.Id, currentFromDb.ReplacedByTokenId);
        Assert.Equal(user.Id, replacementFromDb.UserId);
    }

    [Fact]
    public async Task RevokeAllByUserIdAsync_WhenActiveTokensExist_RevokesAllActive()
    {
        var user = await SeedUserAsync("refresh.revoke@get.io");
        var token1 = CreateRefreshToken(user.Id, "token-hash-a");
        var token2 = CreateRefreshToken(user.Id, "token-hash-b");

        await using (var seedContext = _fixture.CreateDbContext())
        {
            await seedContext.RefreshTokens.AddRangeAsync(token1, token2);
            await seedContext.SaveChangesAsync();
        }

        await using var writeContext = _fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(writeContext);

        var revokedCount = await repository.RevokeAllByUserIdAsync(user.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, revokedCount);

        await using var readContext = _fixture.CreateDbContext();
        var revoked = await readContext.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();

        Assert.All(revoked, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task AddAsync_WhenTokenHashAlreadyExists_ThrowsUniqueConstraintViolationException()
    {
        var user = await SeedUserAsync("refresh.dup@get.io");

        await using var writeContext = _fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(writeContext);

        await repository.AddAsync(CreateRefreshToken(user.Id, "duplicate-hash"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() =>
            repository.AddAsync(CreateRefreshToken(user.Id, "duplicate-hash"), CancellationToken.None));

        Assert.Equal(PersistenceConstraintCodes.RefreshToken.TokenHashUnique, exception.ConstraintCode);
        Assert.Equal(PersistenceConstraintNames.RefreshToken.TokenHashUnique, exception.ConstraintName);
    }

    [Fact]
    public async Task AddAsync_WhenActiveSessionAlreadyExistsForSameDevice_ThrowsUniqueConstraintViolationException()
    {
        var user = await SeedUserAsync("refresh.device-dup@get.io");
        var deviceId = Guid.NewGuid();

        await using var writeContext = _fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(writeContext);

        await repository.AddAsync(CreateRefreshToken(user.Id, "device-hash-1", deviceId), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() =>
            repository.AddAsync(CreateRefreshToken(user.Id, "device-hash-2", deviceId), CancellationToken.None));

        Assert.Equal(PersistenceConstraintCodes.RefreshToken.UserDeviceActiveUnique, exception.ConstraintCode);
        Assert.Equal(PersistenceConstraintNames.RefreshToken.UserDeviceActiveUnique, exception.ConstraintName);
    }

    private async Task<User> SeedUserAsync(string email)
    {
        var user = UserFactory.Valid(email: email, passwordHash: "hash", algorithm: "bcrypt");

        await using var seedContext = _fixture.CreateDbContext();
        await seedContext.Users.AddAsync(user);
        await seedContext.SaveChangesAsync();

        return user;
    }

    private static RefreshToken CreateRefreshToken(Guid userId, string tokenHash, Guid? deviceId = null)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken(userId, deviceId ?? Guid.NewGuid(), tokenHash, now, now.AddDays(30), "127.0.0.1", "integration-test");
    }
}
