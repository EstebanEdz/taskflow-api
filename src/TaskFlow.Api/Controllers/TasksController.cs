using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Common;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class TasksController(ITaskService tasks) : ControllerBase
{
    /// <summary>Crea una tarea dentro de un proyecto (dueño del proyecto o Admin).</summary>
    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        var created = await tasks.CreateAsync(projectId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Lista las tareas de un proyecto con filtros por estado, prioridad y responsable.</summary>
    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<PagedResult<TaskResponse>>> ListByProject(
        Guid projectId,
        [FromQuery] TaskItemStatus? status, [FromQuery] TaskPriority? priority, [FromQuery] Guid? assigneeId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await tasks.ListByProjectAsync(projectId, new TaskFilter(status, priority, assigneeId), page, pageSize, ct));

    /// <summary>Tareas asignadas al usuario autenticado.</summary>
    [HttpGet("tasks/mine")]
    public async Task<ActionResult<PagedResult<TaskResponse>>> Mine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await tasks.ListMineAsync(page, pageSize, ct));

    [HttpGet("tasks/{id:guid}")]
    public async Task<ActionResult<TaskResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await tasks.GetAsync(id, ct));

    [HttpPut("tasks/{id:guid}")]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        Ok(await tasks.UpdateAsync(id, request, ct));

    /// <summary>Cambia el estado respetando las transiciones válidas.</summary>
    [HttpPatch("tasks/{id:guid}/status")]
    public async Task<ActionResult<TaskResponse>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct) =>
        Ok(await tasks.ChangeStatusAsync(id, request.Status!.Value, ct));

    /// <summary>Asigna o desasigna (assigneeId = null) un responsable.</summary>
    [HttpPatch("tasks/{id:guid}/assignee")]
    public async Task<ActionResult<TaskResponse>> Assign(Guid id, AssignTaskRequest request, CancellationToken ct) =>
        Ok(await tasks.AssignAsync(id, request.AssigneeId, ct));

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
