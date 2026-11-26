using TaskFlow.Application.Common;
using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.UseCases.Tasks;

public sealed class CreateTaskUseCase(ITaskRepository tasks, TimeProvider clock)
{
    public async Task<Result<TaskItem>> ExecuteAsync(Guid userId, TaskWriteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TaskItem item;
        try
        {
            var now = clock.GetUtcNow();
            item = new TaskItem(Guid.NewGuid(), userId, request.Title, request.Description,
                TaskStatusParser.Parse(request.Status), request.DueDate, now, now);
        }
        catch (DomainValidationException exception)
        {
            return Result<TaskItem>.Failure(ErrorCode.Validation, exception.Message);
        }

        await tasks.AddAsync(item, cancellationToken);
        return Result<TaskItem>.Success(item);
    }
}

public sealed class ListTasksUseCase(ITaskRepository tasks)
{
    public async Task<Result<IReadOnlyList<TaskItem>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await tasks.ListByUserIdAsync(userId, cancellationToken);
        if (items.Any(item => item.UserId != userId))
            throw new InvalidOperationException("The task repository returned an item belonging to a different user.");
        return Result<IReadOnlyList<TaskItem>>.Success(items);
    }
}

public sealed class GetTaskByIdUseCase(ITaskRepository tasks)
{
    public async Task<Result<TaskItem>> ExecuteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var item = await tasks.GetByIdAsync(taskId, userId, cancellationToken);
        return item is null || item.UserId != userId
            ? Result<TaskItem>.Failure(ErrorCode.NotFound, "Task not found.")
            : Result<TaskItem>.Success(item);
    }
}

public sealed class UpdateTaskUseCase(ITaskRepository tasks, TimeProvider clock)
{
    public async Task<Result<TaskItem>> ExecuteAsync(Guid userId, Guid taskId, TaskWriteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var original = await tasks.GetByIdAsync(taskId, userId, cancellationToken);
        if (original is null || original.UserId != userId)
            return Result<TaskItem>.Failure(ErrorCode.NotFound, "Task not found.");

        TaskItem updated;
        try
        {
            updated = new TaskItem(original.Id, original.UserId, request.Title, request.Description,
                TaskStatusParser.Parse(request.Status), request.DueDate, original.CreatedAt, clock.GetUtcNow());
        }
        catch (DomainValidationException exception)
        {
            return Result<TaskItem>.Failure(ErrorCode.Validation, exception.Message);
        }

        return await tasks.UpdateAsync(updated, userId, cancellationToken)
            ? Result<TaskItem>.Success(updated)
            : Result<TaskItem>.Failure(ErrorCode.NotFound, "Task not found.");
    }
}

public sealed class DeleteTaskUseCase(ITaskRepository tasks)
{
    public async Task<Result<bool>> ExecuteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        return await tasks.DeleteAsync(taskId, userId, cancellationToken)
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(ErrorCode.NotFound, "Task not found.");
    }
}
