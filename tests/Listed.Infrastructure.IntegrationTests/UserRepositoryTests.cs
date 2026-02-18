using Listed.Application.Contracts.Persistence;
using Listed.Infrastructure.Persistence;
using Listed.Infrastructure.Persistence.Repositories;
using Listed.Testing.Factories;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class UserRepositoryTests : IClassFixture<InfrastructureDatabaseFixture>, IAsyncLifetime
{
    private readonly InfrastructureDatabaseFixture _fixture;

    public UserRepositoryTests(InfrastructureDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_WhenValid_PersistsUser()
    {
        await using var writeContext = _fixture.CreateDbContext();
        var repository = new UserRepository(writeContext);
        var user = UserFactory.Valid(email: "infra.persist@test.io", passwordHash: "hash", algorithm: "bcrypt");

        await repository.AddAsync(user, CancellationToken.None);

        await using var readContext = _fixture.CreateDbContext();
        var saved = await readContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == "infra.persist@test.io");

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved!.Id);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ReturnsTrueWhenUserExists()
    {
        await using var seedContext = _fixture.CreateDbContext();
        seedContext.Users.Add(UserFactory.Valid(email: "infra.exists@test.io", passwordHash: "hash", algorithm: "bcrypt"));
        await seedContext.SaveChangesAsync();

        await using var queryContext = _fixture.CreateDbContext();
        var repository = new UserRepository(queryContext);

        var exists = await repository.ExistsByEmailAsync("infra.exists@test.io", CancellationToken.None);
        var missing = await repository.ExistsByEmailAsync("infra.missing@test.io", CancellationToken.None);

        Assert.True(exists);
        Assert.False(missing);
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateEmail_ThrowsUniqueConstraintViolationException()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new UserRepository(context);

        await repository.AddAsync(UserFactory.Valid(email: "infra.dup@test.io", passwordHash: "hash-1", algorithm: "bcrypt"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() =>
            repository.AddAsync(UserFactory.Valid(email: "infra.dup@test.io", passwordHash: "hash-2", algorithm: "bcrypt"), CancellationToken.None));

        Assert.Equal(PersistenceConstraintCodes.User.EmailUnique, exception.ConstraintCode);
        Assert.Equal(PersistenceConstraintNames.User.EmailUnique, exception.ConstraintName);
    }
}
