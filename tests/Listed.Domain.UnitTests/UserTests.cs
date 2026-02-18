using Listed.Domain.Entities;
using Listed.Domain.Exceptions;
using Listed.Testing.Factories;

namespace Listed.Domain.UnitTests;

[Trait("Category", "Unit")]
public sealed class UserTests
{
    [Fact]
    public void Ctor_WhenEmailIsEmpty_ThrowsUserDomainException()
    {
        var action = () => UserFactory.Valid(email: "");

        var exception = Assert.Throws<UserDomainException>(action);
        Assert.Equal("Email cannot be empty.", exception.Message);
    }

    [Fact]
    public void Ctor_WhenEmailIsInvalid_ThrowsUserDomainException()
    {
        var action = () => UserFactory.Valid(email: "invalid-email");

        var exception = Assert.Throws<UserDomainException>(action);
        Assert.Equal("Invalid email format.", exception.Message);
    }

    [Fact]
    public void Ctor_WhenPasswordHashIsEmpty_ThrowsUserDomainException()
    {
        var action = () => UserFactory.Valid(passwordHash: "");

        var exception = Assert.Throws<UserDomainException>(action);
        Assert.Equal("Password hash cannot be empty.", exception.Message);
    }

    [Fact]
    public void Ctor_WhenAlgorithmIsEmpty_ThrowsUserDomainException()
    {
        var action = () => UserFactory.Valid(algorithm: "");

        var exception = Assert.Throws<UserDomainException>(action);
        Assert.Equal("Password algorithm cannot be empty.", exception.Message);
    }

    [Fact]
    public void Ctor_WhenValid_NormalizesEmailAndSetsDefaults()
    {
        var user = UserFactory.Valid(email: "  User@Test.IO  ", passwordHash: "hash", algorithm: "bcrypt");

        Assert.Equal("user@test.io", user.Email);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("bcrypt", user.PasswordAlgorithm);
        Assert.False(user.IsVerified);
        Assert.False(user.IsSoftDeleted);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
        Assert.True(user.PasswordUpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void SetUserInfo_WhenIdsDoNotMatch_ThrowsArgumentException()
    {
        var user = UserFactory.Valid();
        var userInfo = UserInfoFactory.Valid();

        var action = () => user.SetUserInfo(userInfo);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("UserInfo Id must match User Id.", exception.Message);
    }
}
