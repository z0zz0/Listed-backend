using Listed.Application.Users.Commands.CreateUser;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Users.Errors;
using Listed.Domain.Entities;
using Listed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Listed.Testing.Factories;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailIsInvalid_ReturnsInvalidEmail()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.InvalidEmail();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.InvalidEmailCode, result.Error.Code);
        repository.VerifyNoOtherCalls();
        hasher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenPasswordTooShort_ReturnsPasswordTooShort()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.ShortPassword();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.PasswordTooShortCode, result.Error.Code);
        repository.VerifyNoOtherCalls();
        hasher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var repository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        repository
            .Setup(r => r.ExistsByEmailAsync("existing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.Valid(email: " existing@test.io ");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.EmailAlreadyInUseCode, result.Error.Code);
        repository.Verify(r => r.ExistsByEmailAsync("existing@test.io", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        hasher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsSuccessWithUserId()
    {
        var repository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        User? persistedUser = null;

        repository
            .Setup(r => r.ExistsByEmailAsync("new@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => persistedUser = u)
            .Returns(Task.CompletedTask);

        hasher.SetupGet(h => h.AlgorithmName).Returns("bcrypt");
        hasher.Setup(h => h.Hash("StrongPass123!")).Returns("hashed-password");

        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.Valid(email: " New@Test.IO ");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        Assert.NotNull(persistedUser);
        Assert.Equal("new@test.io", persistedUser!.Email);
        Assert.Equal("bcrypt", persistedUser.PasswordAlgorithm);
        Assert.Equal(result.Value, persistedUser.Id);
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolationForEmail_ReturnsConflict()
    {
        var repository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();

        repository
            .Setup(r => r.ExistsByEmailAsync("new@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException(PersistenceConstraintCodes.User.EmailUnique, "unique_index_users_email"));

        hasher.SetupGet(h => h.AlgorithmName).Returns("bcrypt");
        hasher.Setup(h => h.Hash("StrongPass123!")).Returns("hashed-password");

        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.Valid();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.EmailAlreadyInUseCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionOccurs_ReturnsInvalidUserData()
    {
        var repository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();

        repository
            .Setup(r => r.ExistsByEmailAsync("new@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserDomainException("domain-failure"));

        hasher.SetupGet(h => h.AlgorithmName).Returns("bcrypt");
        hasher.Setup(h => h.Hash("StrongPass123!")).Returns("hashed-password");

        var handler = CreateHandler(repository, hasher);
        var command = CreateUserCommandFactory.Valid();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.InvalidUserDataCode, result.Error.Code);
        Assert.Equal("domain-failure", result.Error.Message);
    }

    private static CreateUserCommandHandler CreateHandler(Mock<IUserRepository> repository, Mock<IPasswordHasher> hasher)
    {
        return new CreateUserCommandHandler(
            repository.Object,
            hasher.Object,
            NullLogger<CreateUserCommandHandler>.Instance);
    }
}
