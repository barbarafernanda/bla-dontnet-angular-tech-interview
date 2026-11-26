using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dtos;
using TaskFlow.Application.UseCases.Auth;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tests.Auth;

public class RegisterUserUseCaseTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher hasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task ValidRequest_PersistsHashedUserAndReturnsIt()
    {
        hasher.Hash("Secret123!").Returns("stored-hash");
        var useCase = new RegisterUserUseCase(users, hasher, TaskFixtures.Clock());

        var result = await useCase.ExecuteAsync(new("Demo User", "demo@taskflow.dev", "Secret123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("demo@taskflow.dev");
        result.Value.PasswordHash.Should().Be("stored-hash");
        result.Value.CreatedAt.Should().Be(TaskFixtures.Now);
        await users.Received(1).AddAsync(Arg.Is<User>(u => u.Id == result.Value.Id && u.PasswordHash == "stored-hash" && u.Email == "demo@taskflow.dev"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingEmail_ReturnsConflictWithoutPersisting()
    {
        users.GetByEmailAsync("demo@taskflow.dev", Arg.Any<CancellationToken>()).Returns(new User(Guid.NewGuid(), "Demo User", "demo@taskflow.dev", "hash", TaskFixtures.Now));
        var useCase = new RegisterUserUseCase(users, hasher, TaskFixtures.Clock());

        var result = await useCase.ExecuteAsync(new("Demo User", "demo@taskflow.dev", "Secret123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Conflict);
        await users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(" ", "demo@taskflow.dev", "Secret123!")]
    [InlineData("Demo User", "invalid", "Secret123!")]
    [InlineData("Demo User", "demo@taskflow.dev", "")]
    public async Task InvalidInput_ReturnsValidationErrorWithoutPersisting(string name, string email, string password)
    {
        var useCase = new RegisterUserUseCase(users, hasher, TaskFixtures.Clock());

        var result = await useCase.ExecuteAsync(new RegisterUserRequest(name, email, password));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Validation);
        await users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
