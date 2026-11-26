using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tests.Tasks;

public class CreateTaskUseCaseTests
{
    private readonly ITaskRepository tasks = Substitute.For<ITaskRepository>();

    [Fact]
    public async Task ValidRequest_PersistsTaskOwnedByCaller()
    {
        var result = await new CreateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(TaskFixtures.OwnerId, TaskFixtures.Request());

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(TaskFixtures.OwnerId);
        result.Value.Title.Should().Be("Updated title");
        result.Value.Status.Should().Be(TaskStatus.InProgress);
        result.Value.CreatedAt.Should().Be(TaskFixtures.Now);
        result.Value.UpdatedAt.Should().Be(TaskFixtures.Now);
        await tasks.Received(1).AddAsync(Arg.Is<TaskItem>(t => t.Id == result.Value.Id && t.UserId == TaskFixtures.OwnerId), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "Pending")]
    [InlineData("Valid", "Blocked")]
    public async Task InvalidRequest_ReturnsValidationWithoutPersisting(string title, string status)
    {
        var request = TaskFixtures.Request() with { Title = title, Status = status };
        var result = await new CreateTaskUseCase(tasks, TaskFixtures.Clock()).ExecuteAsync(TaskFixtures.OwnerId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorCode.Validation);
        await tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }
}
