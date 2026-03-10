using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Commands.CompleteSignup;
using Listed.Application.Users.Errors;
using Listed.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Listed.Testing.Factories;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class CompleteSignupCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailAlreadyExistsBeforeCreate_ContinuesWithExistingUser()
    {
        var signupId = Guid.NewGuid();
        var state = CreateVerifiedState("existing@test.io");
        var existingUser = UserFactory.Valid(email: "existing@test.io", passwordHash: "stored-hash", algorithm: "bcrypt");
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var signupVerificationStore = new Mock<ISignupVerificationStore>();

        signupVerificationStore
            .Setup(x => x.GetBySignupIdAsync(signupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);
        userRepository
            .Setup(x => x.ExistsByEmailAsync("existing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userRepository
            .Setup(x => x.GetByEmailAsync("existing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        signupVerificationStore
            .Setup(x => x.SetAsync(signupId, state, TimeSpan.Zero, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(userRepository, passwordHasher, signupVerificationStore);
        var result = await handler.Handle(new CompleteSignupCommand(signupId, "StrongPass123!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existingUser.Id, result.Value!.Id);
        Assert.Equal("existing@test.io", result.Value.Email);

        userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        signupVerificationStore.Verify(x => x.SetAsync(signupId, state, TimeSpan.Zero, It.IsAny<CancellationToken>()), Times.Once);
        passwordHasher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolationForEmail_ContinuesWithExistingUser()
    {
        var signupId = Guid.NewGuid();
        var state = CreateVerifiedState("race@test.io");
        var existingUser = UserFactory.Valid(email: "race@test.io", passwordHash: "stored-hash", algorithm: "bcrypt");
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var signupVerificationStore = new Mock<ISignupVerificationStore>();

        signupVerificationStore
            .Setup(x => x.GetBySignupIdAsync(signupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);
        userRepository
            .Setup(x => x.ExistsByEmailAsync("race@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException(PersistenceConstraintCodes.User.EmailUnique, "unique_index_users_email"));
        userRepository
            .Setup(x => x.GetByEmailAsync("race@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        signupVerificationStore
            .Setup(x => x.SetAsync(signupId, state, TimeSpan.Zero, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        passwordHasher.SetupGet(x => x.AlgorithmName).Returns("bcrypt");
        passwordHasher.Setup(x => x.Hash("StrongPass123!")).Returns("hash");

        var handler = CreateHandler(userRepository, passwordHasher, signupVerificationStore);
        var result = await handler.Handle(new CompleteSignupCommand(signupId, "StrongPass123!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existingUser.Id, result.Value!.Id);
        Assert.Equal("race@test.io", result.Value.Email);

        signupVerificationStore.Verify(x => x.SetAsync(signupId, state, TimeSpan.Zero, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingEmailCannotBeResolved_ReturnsConflict()
    {
        var signupId = Guid.NewGuid();
        var state = CreateVerifiedState("missing@test.io");
        var userRepository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var signupVerificationStore = new Mock<ISignupVerificationStore>();

        signupVerificationStore
            .Setup(x => x.GetBySignupIdAsync(signupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);
        userRepository
            .Setup(x => x.ExistsByEmailAsync("missing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userRepository
            .Setup(x => x.GetByEmailAsync("missing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(userRepository, passwordHasher, signupVerificationStore);
        var result = await handler.Handle(new CompleteSignupCommand(signupId, "StrongPass123!"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.EmailAlreadyInUseCode, result.Error.Code);

        userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        signupVerificationStore.Verify(
            x => x.SetAsync(It.IsAny<Guid>(), It.IsAny<SignupVerificationState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        passwordHasher.VerifyNoOtherCalls();
    }

    private static CompleteSignupCommandHandler CreateHandler(
        Mock<IUserRepository> userRepository,
        Mock<IPasswordHasher> passwordHasher,
        Mock<ISignupVerificationStore> signupVerificationStore)
    {
        return new CompleteSignupCommandHandler(
            userRepository.Object,
            passwordHasher.Object,
            signupVerificationStore.Object,
            NullLogger<CompleteSignupCommandHandler>.Instance);
    }

    private static SignupVerificationState CreateVerifiedState(string email)
    {
        var now = DateTime.UtcNow;
        return new SignupVerificationState(
            email,
            "verification-hash",
            now.AddMinutes(10),
            FailedAttempts: 0,
            IsVerified: true,
            VerifiedAtUtc: now.AddMinutes(-1),
            FirstName: "Jane",
            LastName: "Doe",
            DateOfBirth: DateOnly.FromDateTime(now.AddYears(-25)));
    }
}
