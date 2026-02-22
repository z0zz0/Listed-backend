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
    public async Task Handle_WhenUserIsMissing_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userRepository = new Mock<IUserAuthRepository>();
        var authStateStore = new Mock<IAuthStateStore>(MockBehavior.Strict);

        refreshTokenRepository
            .Setup(x => x.RevokeAllByUserIdAsync(userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        userRepository
            .Setup(x => x.IncrementAuthVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new LogoutAllCommandHandler(
            refreshTokenRepository.Object,
            userRepository.Object,
            authStateStore.Object,
            NullLogger<LogoutAllCommandHandler>.Instance);

        var result = await handler.Handle(
            new LogoutAllCommand(userId, "jti", DateTime.UtcNow.AddMinutes(5)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthError.UserNotFoundCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_RevokesSessionsAndUpdatesAuthVersionCache()
    {
        var user = UserFactory.Valid();

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userRepository = new Mock<IUserAuthRepository>();
        var authStateStore = new Mock<IAuthStateStore>();

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
            authStateStore.Object,
            NullLogger<LogoutAllCommandHandler>.Instance);

        var result = await handler.Handle(
            new LogoutAllCommand(user.Id, "jti", DateTime.UtcNow.AddMinutes(15)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        authStateStore.Verify(x => x.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, It.IsAny<CancellationToken>()), Times.Once);
        authStateStore.Verify(x => x.RevokeAccessTokenAsync("jti", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
