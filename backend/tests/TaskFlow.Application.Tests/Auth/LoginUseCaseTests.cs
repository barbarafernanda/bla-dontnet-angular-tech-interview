using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Auth;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tests.Auth;

public class LoginUseCaseTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator tokens = Substitute.For<IJwtTokenGenerator>();

    [Fact]
    public async Task ValidCredentials_ReturnsGeneratedToken()
    {
        var user = new User(TaskFixtures.OwnerId, "Demo User", "demo@taskflow.dev", "hash", TaskFixtures.Now);
        users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        hasher.Verify("Secret123!", "hash").Returns(true);
        var token = new AuthToken("signed-token", TaskFixtures.Now.AddHours(1));
        tokens.Generate(user).Returns(token);

        var result = await new LoginUseCase(users, hasher, tokens).ExecuteAsync(new(user.Email, "Secret123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(token);
        tokens.Received(1).Generate(user);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsUnauthorizedWithoutGeneratingToken()
    {
        var result = await new LoginUseCase(users, hasher, tokens).ExecuteAsync(new("unknown@taskflow.dev", "Secret123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Unauthorized);
        tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorizedWithoutGeneratingToken()
    {
        var user = new User(TaskFixtures.OwnerId, "Demo User", "demo@taskflow.dev", "hash", TaskFixtures.Now);
        users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        hasher.Verify("wrong", "hash").Returns(false);

        var result = await new LoginUseCase(users, hasher, tokens).ExecuteAsync(new(user.Email, "wrong"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Unauthorized);
        tokens.DidNotReceive().Generate(Arg.Any<User>());
    }
}
