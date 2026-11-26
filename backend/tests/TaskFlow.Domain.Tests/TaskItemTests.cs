using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Tests;

public class TaskItemTests
{
    private static readonly Guid TaskId = Guid.Parse("b5727ac9-3159-42f3-a127-7a8a73454c31");
    private static readonly Guid UserId = Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594");
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DueDate = new(2026, 2, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidValues_PreservesTaskFields()
    {
        var item = new TaskItem(TaskId, UserId, "Write SDD", "Prepare the specification", TaskStatus.Pending, DueDate, CreatedAt, CreatedAt);

        item.Id.Should().Be(TaskId);
        item.UserId.Should().Be(UserId);
        item.Title.Should().Be("Write SDD");
        item.Description.Should().Be("Prepare the specification");
        item.Status.Should().Be(TaskStatus.Pending);
        item.DueDate.Should().Be(DueDate);
        item.CreatedAt.Should().Be(CreatedAt);
        item.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Constructor_WithNullDescription_AcceptsAndPreservesNull()
    {
        var item = CreateTask(description: null);

        item.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Constructor_WithEmptyOrWhitespaceTitle_ThrowsDomainValidationException(string title)
    {
        var act = () => CreateTask(title: title);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Constructor_WithTitleAtMaximumLength_AcceptsTitle()
    {
        var title = new string('a', 120);

        var item = CreateTask(title: title);

        item.Title.Should().Be(title);
    }

    [Fact]
    public void Constructor_WithTitleLongerThanMaximum_ThrowsDomainValidationException()
    {
        var act = () => CreateTask(title: new string('a', 121));

        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(TaskStatus.Pending)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Cancelled)]
    public void ChangeStatus_WithDefinedStatus_UpdatesStatus(TaskStatus status)
    {
        var item = CreateTask(status: TaskStatus.Pending);

        item.ChangeStatus(status);

        item.Status.Should().Be(status);
    }

    [Fact]
    public void Constructor_WithUndefinedStatus_ThrowsInvalidTaskStatusException()
    {
        var act = () => CreateTask(status: (TaskStatus)999);

        act.Should().Throw<InvalidTaskStatusException>();
    }

    [Fact]
    public void ChangeStatus_WithUndefinedStatus_ThrowsAndPreservesPreviousStatus()
    {
        var item = CreateTask(status: TaskStatus.InProgress);
        var act = () => item.ChangeStatus((TaskStatus)999);

        act.Should().Throw<InvalidTaskStatusException>();
        item.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public void MarkUpdated_UsesInjectedTimeAndPreservesCreationTime()
    {
        var item = CreateTask();
        var updatedAt = CreatedAt.AddHours(3);
        var clock = new FixedTimeProvider(updatedAt);

        item.MarkUpdated(clock);

        item.UpdatedAt.Should().Be(updatedAt);
        item.CreatedAt.Should().Be(CreatedAt);
    }

    private static TaskItem CreateTask(string title = "Write SDD", string? description = null, TaskStatus status = TaskStatus.Pending) =>
        new(TaskId, UserId, title, description, status, DueDate, CreatedAt, CreatedAt);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
