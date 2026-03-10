using Listed.Application.Auth.Commands.Login;
using Listed.Application.Auth.Errors;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Entities;
using Listed.Testing.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailIsInvalid_ReturnsInvalidEmail()
    {
        var userRepository = new Mock<IUserAuthRepository>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var accessTokenService = new Mock<IAccessTokenService>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var command = new LoginCommand("invalid-email", "StrongPass123!", Guid.NewGuid(), null, "127.0.0.1", "unit-test");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.InvalidEmailCode, result.Error.Code);
        userRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreInvalid_ReturnsInvalidCredentials()
    {
        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var accessTokenService = new Mock<IAccessTokenService>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        userRepository
            .Setup(x => x.GetByEmailForAuthAsync("missing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var command = new LoginCommand(" missing@test.io ", "StrongPass123!", Guid.NewGuid(), null, "127.0.0.1", "unit-test");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.InvalidCredentialsCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsAccessAndRefreshTokens()
    {
        var now = DateTime.UtcNow;
        var deviceId = Guid.NewGuid();
        var user = UserFactory.Valid(email: "valid@test.io", passwordHash: "stored-hash", algorithm: "bcrypt");

        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var accessTokenService = new Mock<IAccessTokenService>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>();

        userRepository
            .Setup(x => x.GetByEmailForAuthAsync("valid@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(x => x.Verify("StrongPass123!", "stored-hash"))
            .Returns(true);
        accessTokenService
            .Setup(x => x.Create(user.Id, It.IsAny<Guid>(), user.Email, user.AuthInfo.AuthVersion, It.IsAny<DateTime>()))
            .Returns(new AccessTokenResult("access-token", now.AddMinutes(15), 900));
        refreshTokenService
            .Setup(x => x.GenerateToken())
            .Returns("refresh-token");
        refreshTokenService
            .Setup(x => x.HashToken("refresh-token"))
            .Returns("refresh-token-hash");
        refreshTokenRepository
            .Setup(x => x.RevokeExpiredByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        refreshTokenRepository
            .Setup(x => x.GetActiveByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        refreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var command = new LoginCommand("valid@test.io", "StrongPass123!", deviceId, null, "127.0.0.1", "unit-test");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access-token", result.Value!.AccessToken.Token);
        Assert.Equal("refresh-token", result.Value.RefreshToken);

        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenActiveSessionExistsOnSameDeviceAndCookieMissing_RotatesSessionAndReturnsSuccess()
    {
        var now = DateTime.UtcNow;
        var deviceId = Guid.NewGuid();
        var user = UserFactory.Valid(email: "dupe@test.io", passwordHash: "stored-hash", algorithm: "bcrypt");
        var existingRefreshTokenHash = "existing-active-hash";
        var activeToken = new RefreshToken(
            user.Id,
            deviceId,
            Guid.NewGuid(),
            existingRefreshTokenHash,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            "127.0.0.1",
            "unit-test");

        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var accessTokenService = new Mock<IAccessTokenService>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>();

        userRepository
            .Setup(x => x.GetByEmailForAuthAsync("dupe@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(x => x.Verify("StrongPass123!", "stored-hash"))
            .Returns(true);
        accessTokenService
            .Setup(x => x.Create(user.Id, It.IsAny<Guid>(), user.Email, user.AuthInfo.AuthVersion, It.IsAny<DateTime>()))
            .Returns(new AccessTokenResult("access-token", now.AddMinutes(15), 900));
        refreshTokenService
            .Setup(x => x.GenerateToken())
            .Returns("new-refresh-token");
        refreshTokenService
            .Setup(x => x.HashToken("new-refresh-token"))
            .Returns("new-refresh-token-hash");
        refreshTokenRepository
            .Setup(x => x.RevokeExpiredByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        refreshTokenRepository
            .Setup(x => x.GetActiveByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeToken);
        authStateStore
            .Setup(x => x.IsSessionRevokedAsync(activeToken.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        refreshTokenRepository
            .Setup(x => x.RotateAsync(activeToken.Id, It.IsAny<RefreshToken>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var command = new LoginCommand("dupe@test.io", "StrongPass123!", deviceId, null, "127.0.0.1", "unit-test");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access-token", result.Value!.AccessToken.Token);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.True(result.Value.RefreshTokenExpiresAtUtc > now);
        refreshTokenRepository.Verify(
            x => x.RotateAsync(activeToken.Id, It.IsAny<RefreshToken>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenActiveSessionExistsButSessionIsRevoked_CreatesNewSession()
    {
        var now = DateTime.UtcNow;
        var deviceId = Guid.NewGuid();
        var user = UserFactory.Valid(email: "revoked@test.io", passwordHash: "stored-hash", algorithm: "bcrypt");
        var activeToken = new RefreshToken(
            user.Id,
            deviceId,
            Guid.NewGuid(),
            "existing-active-hash",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            "127.0.0.1",
            "unit-test");

        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var accessTokenService = new Mock<IAccessTokenService>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>();

        userRepository
            .Setup(x => x.GetByEmailForAuthAsync("revoked@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(x => x.Verify("StrongPass123!", "stored-hash"))
            .Returns(true);
        accessTokenService
            .Setup(x => x.Create(user.Id, It.IsAny<Guid>(), user.Email, user.AuthInfo.AuthVersion, It.IsAny<DateTime>()))
            .Returns(new AccessTokenResult("access-token", now.AddMinutes(15), 900));
        refreshTokenService
            .Setup(x => x.GenerateToken())
            .Returns("new-refresh-token");
        refreshTokenService
            .Setup(x => x.HashToken("new-refresh-token"))
            .Returns("new-refresh-token-hash");
        refreshTokenRepository
            .Setup(x => x.RevokeExpiredByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        refreshTokenRepository
            .Setup(x => x.GetActiveByUserAndDeviceAsync(user.Id, deviceId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeToken);
        authStateStore
            .Setup(x => x.IsSessionRevokedAsync(activeToken.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        refreshTokenRepository
            .Setup(x => x.RevokeAsync(activeToken.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        refreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var command = new LoginCommand("revoked@test.io", "StrongPass123!", deviceId, null, "127.0.0.1", "unit-test");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access-token", result.Value!.AccessToken.Token);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);

        accessTokenService.Verify(
            x => x.Create(
                user.Id,
                It.Is<Guid>(sessionId => sessionId != activeToken.SessionId),
                user.Email,
                user.AuthInfo.AuthVersion,
                It.IsAny<DateTime>()),
            Times.Once);
        refreshTokenRepository.Verify(
            x => x.RevokeAsync(activeToken.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshTokenRepository.Verify(
            x => x.RotateAsync(It.IsAny<Guid>(), It.IsAny<RefreshToken>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static LoginCommandHandler CreateHandler(
        Mock<IUserAuthRepository> userRepository,
        Mock<IRefreshTokenRepository> refreshTokenRepository,
        Mock<IPasswordHasher> passwordHasher,
        Mock<IAccessTokenService> accessTokenService,
        Mock<IRefreshTokenService> refreshTokenService,
        Mock<IAuthSettings> authSettings,
        Mock<IAuthStateStore> authStateStore)
    {
        return new LoginCommandHandler(
            userRepository.Object,
            refreshTokenRepository.Object,
            passwordHasher.Object,
            accessTokenService.Object,
            refreshTokenService.Object,
            authSettings.Object,
            authStateStore.Object,
            NullLogger<LoginCommandHandler>.Instance);
    }

    private static Mock<IAuthSettings> CreateAuthSettingsMock()
    {
        var settings = new Mock<IAuthSettings>();
        settings.SetupGet(x => x.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        settings.SetupGet(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        settings.SetupGet(x => x.RefreshTokenCookieName).Returns("listed.refresh_token");
        return settings;
    }
}
