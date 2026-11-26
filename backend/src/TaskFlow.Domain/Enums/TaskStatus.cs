using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Enums;

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public static class TaskStatusParser
{
    public static TaskStatus Parse(string value)
    {
        return value switch
        {
            nameof(TaskStatus.Pending) => TaskStatus.Pending,
            nameof(TaskStatus.InProgress) => TaskStatus.InProgress,
            nameof(TaskStatus.Completed) => TaskStatus.Completed,
            nameof(TaskStatus.Cancelled) => TaskStatus.Cancelled,
            _ => throw new InvalidTaskStatusException("Invalid task status.")
        };
    }
}
