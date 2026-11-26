using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Shared;
using TaskFlow.Application.Dtos;
using TaskFlow.Application.UseCases.Tasks;

namespace TaskFlow.Tasks.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = "ValidUser")]
public sealed class TasksController(ListTasksUseCase list, GetTaskByIdUseCase get, CreateTaskUseCase create, UpdateTaskUseCase update, DeleteTaskUseCase delete) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await list.ExecuteAsync(ApiConfiguration.UserId(User), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiConfiguration.Error(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await get.ExecuteAsync(ApiConfiguration.UserId(User), id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiConfiguration.Error(this, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await create.ExecuteAsync(ApiConfiguration.UserId(User), request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value) : ApiConfiguration.Error(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TaskWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await update.ExecuteAsync(ApiConfiguration.UserId(User), id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiConfiguration.Error(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await delete.ExecuteAsync(ApiConfiguration.UserId(User), id, cancellationToken);
        return result.IsSuccess ? NoContent() : ApiConfiguration.Error(this, result);
    }
}
