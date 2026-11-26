using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.Tests.Tasks;

public class ListTasksUseCaseTests
{
    [Fact]
    public async Task Execute_QueriesOnlyTheCallerAndReturnsItsTasks()
    {
        var tasks = Substitute.For<ITaskRepository>();
        IReadOnlyList<TaskItem> items = [TaskFixtures.Item()];
        tasks.ListByUserIdAsync(TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(items);

        var result = await new ListTasksUseCase(tasks).ExecuteAsync(TaskFixtures.OwnerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(items);
        await tasks.Received(1).ListByUserIdAsync(TaskFixtures.OwnerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenNoTasks_ReturnsEmptyList()
    {
        var tasks = Substitute.For<ITaskRepository>();
        tasks.ListByUserIdAsync(TaskFixtures.OwnerId, Arg.Any<CancellationToken>()).Returns(Array.Empty<TaskItem>());

        var result = await new ListTasksUseCase(tasks).ExecuteAsync(TaskFixtures.OwnerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
