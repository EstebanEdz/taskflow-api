using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default);
    Task<PagedResult<TaskResponse>> ListByProjectAsync(Guid projectId, TaskFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<TaskResponse>> ListMineAsync(int page, int pageSize, CancellationToken ct = default);
    Task<TaskResponse> GetAsync(Guid id, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> ChangeStatusAsync(Guid id, TaskItemStatus newStatus, CancellationToken ct = default);
    Task<TaskResponse> AssignAsync(Guid id, Guid? assigneeId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class TaskService(AppDbContext db, ICurrentUser current) : ITaskService
{
    // Máquina de estados: qué transiciones son válidas.
    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> Transitions = new()
    {
        [TaskItemStatus.Todo]       = [TaskItemStatus.InProgress],
        [TaskItemStatus.InProgress] = [TaskItemStatus.Todo, TaskItemStatus.InReview, TaskItemStatus.Done],
        [TaskItemStatus.InReview]   = [TaskItemStatus.InProgress, TaskItemStatus.Done],
        [TaskItemStatus.Done]       = [TaskItemStatus.InProgress] // reabrir
    };

    private static IQueryable<TaskResponse> ToResponse(IQueryable<TaskItem> q) =>
        q.Select(t => new TaskResponse(
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.ProjectId,
            t.AssigneeId, t.Assignee != null ? t.Assignee.FullName : null, t.CreatedAt, t.UpdatedAt));

    private bool IsAdmin => current.Role == UserRole.Admin;

    private async Task<Project> GetProjectAsync(Guid projectId, CancellationToken ct) =>
        await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct)
        ?? throw new NotFoundException("Proyecto no encontrado.");

    private async Task<TaskItem> LoadTaskAsync(Guid id, CancellationToken ct) =>
        await db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException("Tarea no encontrada.");

    private bool CanManage(TaskItem t) => IsAdmin || t.Project.OwnerId == current.Id;

    private static void ValidateDueDate(DateTime? due)
    {
        if (due is not null && due.Value.ToUniversalTime().Date < DateTime.UtcNow.Date)
            throw new BadRequestException("La fecha de vencimiento no puede estar en el pasado.");
    }

    private async Task EnsureUserExistsAsync(Guid? userId, CancellationToken ct)
    {
        if (userId is null) return;
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            throw new BadRequestException("El responsable indicado no existe.");
    }

    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var project = await GetProjectAsync(projectId, ct);
        if (!IsAdmin && project.OwnerId != current.Id) throw new ForbiddenException();

        ValidateDueDate(request.DueDate);
        await EnsureUserExistsAsync(request.AssigneeId, ct);

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate?.ToUniversalTime(),
            AssigneeId = request.AssigneeId
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return await GetAsync(task.Id, ct);
    }

    public async Task<PagedResult<TaskResponse>> ListByProjectAsync(
        Guid projectId, TaskFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        await GetProjectAsync(projectId, ct);
        (page, pageSize) = PagedResult<TaskResponse>.Normalize(page, pageSize);

        var query = db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);
        if (filter.Status is not null) query = query.Where(t => t.Status == filter.Status);
        if (filter.Priority is not null) query = query.Where(t => t.Priority == filter.Priority);
        if (filter.AssigneeId is not null) query = query.Where(t => t.AssigneeId == filter.AssigneeId);

        var total = await query.CountAsync(ct);
        var items = await ToResponse(query
                .OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate)
                .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync(ct);
        return new PagedResult<TaskResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<TaskResponse>> ListMineAsync(int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<TaskResponse>.Normalize(page, pageSize);
        var query = db.Tasks.AsNoTracking().Where(t => t.AssigneeId == current.Id);
        var total = await query.CountAsync(ct);
        var items = await ToResponse(query
                .OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
                .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync(ct);
        return new PagedResult<TaskResponse>(items, page, pageSize, total);
    }

    public async Task<TaskResponse> GetAsync(Guid id, CancellationToken ct = default) =>
        await ToResponse(db.Tasks.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Tarea no encontrada.");

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException();
        ValidateDueDate(request.DueDate);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.DueDate = request.DueDate?.ToUniversalTime();
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, TaskItemStatus newStatus, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);

        // Pueden cambiar el estado: Admin, dueño del proyecto o el responsable de la tarea.
        if (!CanManage(task) && task.AssigneeId != current.Id) throw new ForbiddenException();

        if (task.Status == newStatus)
            throw new BadRequestException($"La tarea ya está en estado {newStatus}.");
        if (!Transitions[task.Status].Contains(newStatus))
            throw new BadRequestException($"Transición inválida: {task.Status} → {newStatus}.");

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<TaskResponse> AssignAsync(Guid id, Guid? assigneeId, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException();
        await EnsureUserExistsAsync(assigneeId, ct);

        task.AssigneeId = assigneeId;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }
}
