using Listed.Application.Auth.Commands.LogoutAll;
using Listed.Application.Auth.Errors;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Testing.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class LogoutAllCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionIsMissing_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var refreshToken = new Listed.Domain.Entities.RefreshToken(
            userId,
            Guid.NewGuid(),
            sessionId,
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            null,
            null);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        refreshTokenService
            .Setup(x => x.HashToken("token"))
            .Returns("hash");
        refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        refreshTokenRepository
            .Setup(x => x.RevokeAllByUserIdAsync(userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        userRepository
            .Setup(x => x.IncrementAuthVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new LogoutAllCommandHandler(
            refreshTokenRepository.Object,
            userRepository.Object,
            refreshTokenService.Object,
            authStateStore.Object,
            NullLogger<LogoutAllCommandHandler>.Instance);

        var result = await handler.Handle(
            new LogoutAllCommand(userId, "token", sessionId, "jti", DateTime.UtcNow.AddMinutes(5)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.SessionNotFoundCode, result.Error.Code);
        Assert.Equal($"AuthSession for Userid '{userId}' was not found.", result.Error.Message);
    }

    [Fact]
    public async Task Handle_WhenValid_RevokesSessionsAndUpdatesAuthVersionCache()
    {
        var user = UserFactory.Valid();
        var sessionId = Guid.NewGuid();
        var refreshToken = new Listed.Domain.Entities.RefreshToken(
            user.Id,
            Guid.NewGuid(),
            sessionId,
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            null,
            null);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userRepository = new Mock<IUserAuthRepository>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authStateStore = new Mock<IAuthStateStore>();

        refreshTokenService
            .Setup(x => x.HashToken("token"))
            .Returns("hash");
        refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        refreshTokenRepository
            .Setup(x => x.RevokeAllByUserIdAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        userRepository
            .Setup(x => x.IncrementAuthVersionAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userRepository
            .Setup(x => x.GetByIdForAuthAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        authStateStore
            .Setup(x => x.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authStateStore
            .Setup(x => x.RevokeAccessTokenAsync("jti", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LogoutAllCommandHandler(
            refreshTokenRepository.Object,
            userRepository.Object,
            refreshTokenService.Object,
            authStateStore.Object,
            NullLogger<LogoutAllCommandHandler>.Instance);

        var result = await handler.Handle(
            new LogoutAllCommand(user.Id, "token", sessionId, "jti", DateTime.UtcNow.AddMinutes(15)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        authStateStore.Verify(x => x.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, It.IsAny<CancellationToken>()), Times.Once);
        authStateStore.Verify(x => x.RevokeAccessTokenAsync("jti", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRefreshSessionDoesNotMatch_ReturnsUnauthorized()
    {
        var user = UserFactory.Valid();
        var refreshToken = new Listed.Domain.Entities.RefreshToken(
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            null,
            null);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userRepository = new Mock<IUserAuthRepository>(MockBehavior.Strict);
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        refreshTokenService
            .Setup(x => x.HashToken("token"))
            .Returns("hash");
        refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var handler = new LogoutAllCommandHandler(
            refreshTokenRepository.Object,
            userRepository.Object,
            refreshTokenService.Object,
            authStateStore.Object,
            NullLogger<LogoutAllCommandHandler>.Instance);

        var result = await handler.Handle(
            new LogoutAllCommand(user.Id, "token", Guid.NewGuid(), "jti", DateTime.UtcNow.AddMinutes(15)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.InvalidRefreshTokenCode, result.Error.Code);
        refreshTokenRepository.Verify(
            x => x.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
