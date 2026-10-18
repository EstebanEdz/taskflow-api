using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<PagedResult<ProjectResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ProjectService(AppDbContext db, ICurrentUser current) : IProjectService
{
    // Proyección directa a DTO: una sola consulta SQL, sin cargar entidades completas.
    private static IQueryable<ProjectResponse> ToResponse(IQueryable<Project> q) =>
        q.Select(p => new ProjectResponse(
            p.Id, p.Name, p.Description, p.OwnerId, p.Owner.FullName, p.CreatedAt,
            p.Tasks.Count(), p.Tasks.Count(t => t.Status == TaskItemStatus.Done)));

    private bool CanManage(Project p) => current.Role == UserRole.Admin || p.OwnerId == current.Id;

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        if (current.Role == UserRole.Member)
            throw new ForbiddenException("Solo Manager o Admin pueden crear proyectos.");

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            OwnerId = current.Id
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return await GetAsync(project.Id, ct);
    }

    public async Task<PagedResult<ProjectResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<ProjectResponse>.Normalize(page, pageSize);
        var query = db.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search.Trim()));

        var total = await query.CountAsync(ct);
        var items = await ToResponse(query.OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync(ct);
        return new PagedResult<ProjectResponse>(items, page, pageSize, total);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        await ToResponse(db.Projects.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Proyecto no encontrado.");

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException("Proyecto no encontrado.");
        if (!CanManage(project)) throw new ForbiddenException();

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException("Proyecto no encontrado.");
        if (!CanManage(project)) throw new ForbiddenException();

        // Regla de negocio ("cuando corresponda"): no se elimina con tareas sin terminar.
        var hasOpenTasks = await db.Tasks.AnyAsync(t => t.ProjectId == id && t.Status != TaskItemStatus.Done, ct);
        if (hasOpenTasks)
            throw new ConflictException("No se puede eliminar un proyecto con tareas pendientes.");

        db.Projects.Remove(project); // las tareas Done se eliminan en cascada
        await db.SaveChangesAsync(ct);
    }
}
