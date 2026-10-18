using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Common;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(IProjectService projects) : ControllerBase
{
    /// <summary>Crea un proyecto (Manager o Admin).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var created = await projects.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Lista proyectos con búsqueda por nombre y paginación.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectResponse>>> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await projects.ListAsync(search, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await projects.GetAsync(id, ct));

    /// <summary>Actualiza un proyecto (dueño o Admin).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id, UpdateProjectRequest request, CancellationToken ct) =>
        Ok(await projects.UpdateAsync(id, request, ct));

    /// <summary>Elimina un proyecto si no tiene tareas pendientes (dueño o Admin).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await projects.DeleteAsync(id, ct);
        return NoContent();
    }
}
