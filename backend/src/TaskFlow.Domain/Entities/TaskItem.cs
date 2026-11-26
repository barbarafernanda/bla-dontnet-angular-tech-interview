using TaskFlow.Domain.Exceptions;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Entities;

public sealed class TaskItem
{
    public TaskItem(Guid id, Guid userId, string title, string? description, TaskStatus status, DateTimeOffset dueDate, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 120)
            throw new DomainValidationException("Title is required and must not exceed 120 characters.");

        if (description is { Length: > 8000 })
            throw new DomainValidationException("Description must not exceed 8000 characters.");

        ValidateStatus(status);
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset DueDate { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void ChangeStatus(TaskStatus status)
    {
        ValidateStatus(status);
        Status = status;
    }

    public void MarkUpdated(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        UpdatedAt = timeProvider.GetUtcNow();
    }

    private static void ValidateStatus(TaskStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new InvalidTaskStatusException($"Invalid task status: {status}.");
    }
}
