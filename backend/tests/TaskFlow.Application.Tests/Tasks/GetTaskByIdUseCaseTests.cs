using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tests.Tasks;

public class GetTaskByIdUseCaseTests
{
    [Fact]
    public async Task OwnedTask_ReturnsTaskAndScopesLookupToCaller()
    {
        var tasks = Substitute.For<ITaskRepository>();
        var item = TaskFixtures.Item();
        tasks.GetByIdAsync(item.Id, TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await new GetTaskByIdUseCase(tasks).ExecuteAsync(TaskFixtures.OwnerId, item.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(item);
        await tasks.Received(1).GetByIdAsync(item.Id, TaskFixtures.OwnerId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrOtherUsersTask_ReturnsNotFound(bool otherUser)
    {
        var tasks = Substitute.For<ITaskRepository>();
        var caller = otherUser ? TaskFixtures.OtherUserId : TaskFixtures.OwnerId;
        var id = otherUser ? TaskFixtures.TaskId : Guid.NewGuid();

        var result = await new GetTaskByIdUseCase(tasks).ExecuteAsync(caller, id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.NotFound);
        await tasks.Received(1).GetByIdAsync(id, caller, Arg.Any<CancellationToken>());
    }
}
