using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tests.Tasks;

public class UpdateTaskUseCaseTests
{
    private readonly ITaskRepository tasks = Substitute.For<ITaskRepository>();

    [Fact]
    public async Task OwnedTask_PersistsUpdatedFieldsAndTimestamps()
    {
        var original = TaskFixtures.Item();
        tasks.GetByIdAsync(original.Id, TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(original);
        tasks.UpdateAsync(Arg.Any<TaskItem>(), TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new UpdateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(TaskFixtures.OwnerId, original.Id, TaskFixtures.Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(original.Id);
        result.Value.UserId.Should().Be(TaskFixtures.OwnerId);
        result.Value.Title.Should().Be("Updated title");
        result.Value.Description.Should().Be("Updated description");
        result.Value.Status.Should().Be(TaskStatus.InProgress);
        result.Value.DueDate.Should().Be(TaskFixtures.DueDate);
        result.Value.CreatedAt.Should().Be(original.CreatedAt);
        result.Value.UpdatedAt.Should().Be(TaskFixtures.Now);
        await tasks.Received(1).UpdateAsync(Arg.Is<TaskItem>(item => item.Id == original.Id && item.UserId == TaskFixtures.OwnerId && item.Title == "Updated title"), TaskFixtures.OwnerId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrOtherUsersTask_ReturnsNotFoundWithoutUpdate(bool otherUser)
    {
        var caller = otherUser ? TaskFixtures.OtherUserId : TaskFixtures.OwnerId;
        var id = otherUser ? TaskFixtures.TaskId : Guid.NewGuid();

        var result = await new UpdateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(caller, id, TaskFixtures.Request());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.NotFound);
        await tasks.Received(1).GetByIdAsync(id, caller, Arg.Any<CancellationToken>());
        await tasks.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidStatus_ReturnsValidationWithoutUpdate()
    {
        tasks.GetByIdAsync(TaskFixtures.TaskId, TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(TaskFixtures.Item());
        var request = TaskFixtures.Request() with { Status = "Blocked" };

        var result = await new UpdateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(TaskFixtures.OwnerId, TaskFixtures.TaskId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Validation);
        await tasks.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConcurrentRemovalDuringUpdate_ReturnsNotFound()
    {
        tasks.GetByIdAsync(TaskFixtures.TaskId, TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(TaskFixtures.Item());
        tasks.UpdateAsync(Arg.Any<TaskItem>(), TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new UpdateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(TaskFixtures.OwnerId, TaskFixtures.TaskId, TaskFixtures.Request());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.NotFound);
    }
}
