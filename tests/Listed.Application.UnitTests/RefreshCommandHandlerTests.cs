using Listed.Application.Auth.Commands.Refresh;
using Listed.Application.Auth.Errors;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Entities;
using Listed.Testing.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class RefreshCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRefreshTokenMissing_ReturnsMissingRefreshToken()
    {
        var userRepository = new Mock<IUserAuthRepository>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var accessTokenService = new Mock<IAccessTokenService>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var result = await handler.Handle(new RefreshCommand(null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.MissingRefreshTokenCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDeviceIdMissing_ReturnsInvalidRefreshToken()
    {
        var userRepository = new Mock<IUserAuthRepository>(MockBehavior.Strict);
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var accessTokenService = new Mock<IAccessTokenService>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>(MockBehavior.Strict);
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var result = await handler.Handle(new RefreshCommand("token", null, "127.0.0.1", "unit"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.InvalidRefreshTokenCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenAlreadyRevoked_ReturnsReusedTokenAndRevokesAllSessions()
    {
        var user = UserFactory.Valid();
        var revokedRefreshToken = new RefreshToken(
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            null,
            null);
        SetPrivateProperty(revokedRefreshToken, nameof(RefreshToken.RevokedAt), DateTime.UtcNow.AddMinutes(-1));

        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var accessTokenService = new Mock<IAccessTokenService>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authSettings = CreateAuthSettingsMock();
        var authStateStore = new Mock<IAuthStateStore>();

        refreshTokenService.Setup(x => x.HashToken("token")).Returns("hash");
        refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedRefreshToken);
        refreshTokenRepository
            .Setup(x => x.RevokeAllByUserIdAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        userRepository
            .Setup(x => x.IncrementAuthVersionAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userRepository
            .Setup(x => x.GetByIdForAuthAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        authStateStore
            .Setup(x => x.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            userRepository,
            refreshTokenRepository,
            accessTokenService,
            refreshTokenService,
            authSettings,
            authStateStore);

        var result = await handler.Handle(new RefreshCommand("token", revokedRefreshToken.DeviceId, "127.0.0.1", "unit"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.ReusedRefreshTokenCode, result.Error.Code);
        refreshTokenRepository.Verify(x => x.RevokeAllByUserIdAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepository.Verify(x => x.IncrementAuthVersionAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RefreshCommandHandler CreateHandler(
        Mock<IUserAuthRepository> userRepository,
        Mock<IRefreshTokenRepository> refreshTokenRepository,
        Mock<IAccessTokenService> accessTokenService,
        Mock<IRefreshTokenService> refreshTokenService,
        Mock<IAuthSettings> authSettings,
        Mock<IAuthStateStore> authStateStore)
    {
        return new RefreshCommandHandler(
            userRepository.Object,
            refreshTokenRepository.Object,
            accessTokenService.Object,
            refreshTokenService.Object,
            authSettings.Object,
            authStateStore.Object,
            NullLogger<RefreshCommandHandler>.Instance);
    }

    private static Mock<IAuthSettings> CreateAuthSettingsMock()
    {
        var settings = new Mock<IAuthSettings>();
        settings.SetupGet(x => x.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        settings.SetupGet(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        settings.SetupGet(x => x.RefreshTokenCookieName).Returns("listed.refresh_token");
        return settings;
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property!.SetValue(target, value);
    }
}
