using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tests;

internal static class TaskFixtures
{
    public static readonly Guid OwnerId = Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594");
    public static readonly Guid OtherUserId = Guid.Parse("fdf16f1f-e7a3-4fe6-8a8b-04d36da2797b");
    public static readonly Guid TaskId = Guid.Parse("b5727ac9-3159-42f3-a127-7a8a73454c31");
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset DueDate = Now.AddDays(7);

    public static TaskItem Item() => new(TaskId, OwnerId, "Original title", "Original description", TaskStatus.Pending, DueDate, Now.AddDays(-1), Now.AddDays(-1));
    public static TaskWriteRequest Request() => new("Updated title", "Updated description", "InProgress", DueDate);
    public static TimeProvider Clock() => new FixedClock();

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
