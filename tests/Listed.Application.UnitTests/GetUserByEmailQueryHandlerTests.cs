using Listed.Application.Contracts.Persistence;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Queries.GetUserByEmail;
using Listed.Domain.Entities;
using Listed.Testing.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Listed.Application.UnitTests;

[Trait("Category", "Unit")]
public sealed class GetUserByEmailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailIsInvalid_ReturnsInvalidEmail()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var handler = CreateHandler(repository);
        var query = GetUserByEmailQueryFactory.InvalidEmail();

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.InvalidEmailCode, result.Error.Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenEmailIsEmpty_ReturnsInvalidEmail()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var handler = CreateHandler(repository);
        var query = GetUserByEmailQueryFactory.EmptyEmail();

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.InvalidEmailCode, result.Error.Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByEmailAsync("missing@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(repository);
        var query = GetUserByEmailQueryFactory.Valid(" Missing@Test.IO ");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserError.UserNotFoundByEmailCode, result.Error.Code);
        repository.Verify(r => r.GetByEmailAsync("missing@test.io", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsMappedResult()
    {
        var repository = new Mock<IUserRepository>();
        var seededUser = UserFactory.Valid(
            email: "mapped@test.io",
            includeUserInfo: true,
            photoCount: 2);

        repository
            .Setup(r => r.GetByEmailAsync("mapped@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(seededUser);

        var handler = CreateHandler(repository);
        var query = GetUserByEmailQueryFactory.Valid(" mapped@test.io ");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(seededUser.Id, result.Value!.Id);
        Assert.Equal("mapped@test.io", result.Value.Email);
        Assert.Equal(seededUser.IsVerified, result.Value.IsVerified);
        Assert.Equal(seededUser.IsSoftDeleted, result.Value.IsSoftDeleted);

        Assert.NotNull(result.Value.UserInfo);
        Assert.Equal("SE", result.Value.UserInfo!.Nationality);
        Assert.Equal("John", result.Value.UserInfo.FirstName);
        Assert.Equal("Doe", result.Value.UserInfo.LastName);
        Assert.Equal("0700000000", result.Value.UserInfo.PhoneNumber);
        Assert.True(result.Value.UserInfo.HasPhonePrefix);

        Assert.Equal(2, result.Value.Photos.Count);
        Assert.Equal(1, result.Value.Photos.First().SortOrder);
        Assert.Equal(2, result.Value.Photos.Last().SortOrder);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoUserInfo_ReturnsNullUserInfo()
    {
        var repository = new Mock<IUserRepository>();
        var seededUser = UserFactory.Valid(
            email: "nouserinfo@test.io",
            includeUserInfo: false,
            photoCount: 1);

        repository
            .Setup(r => r.GetByEmailAsync("nouserinfo@test.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(seededUser);

        var handler = CreateHandler(repository);
        var query = GetUserByEmailQueryFactory.Valid(" nouserinfo@test.io ");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value!.UserInfo);
        Assert.Single(result.Value.Photos);
    }

    private static GetUserByEmailQueryHandler CreateHandler(Mock<IUserRepository> repository)
    {
        return new GetUserByEmailQueryHandler(
            repository.Object,
            NullLogger<GetUserByEmailQueryHandler>.Instance);
    }
}
