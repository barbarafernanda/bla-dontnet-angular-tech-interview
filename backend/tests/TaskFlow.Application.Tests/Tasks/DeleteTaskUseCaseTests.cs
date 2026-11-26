using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tests.Tasks;

public class DeleteTaskUseCaseTests
{
    [Fact]
    public async Task OwnedTask_DeletesUsingCallerId()
    {
        var tasks = Substitute.For<ITaskRepository>();
        tasks.DeleteAsync(TaskFixtures.TaskId, TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteTaskUseCase(tasks).ExecuteAsync(TaskFixtures.OwnerId, TaskFixtures.TaskId);

        result.IsSuccess.Should().BeTrue();
        await tasks.Received(1).DeleteAsync(TaskFixtures.TaskId, TaskFixtures.OwnerId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrOtherUsersTask_ReturnsNotFound(bool otherUser)
    {
        var tasks = Substitute.For<ITaskRepository>();
        var caller = otherUser ? TaskFixtures.OtherUserId : TaskFixtures.OwnerId;
        var id = otherUser ? TaskFixtures.TaskId : Guid.NewGuid();
        tasks.DeleteAsync(id, caller, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new DeleteTaskUseCase(tasks).ExecuteAsync(caller, id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.NotFound);
        await tasks.Received(1).DeleteAsync(id, caller, Arg.Any<CancellationToken>());
    }
}
