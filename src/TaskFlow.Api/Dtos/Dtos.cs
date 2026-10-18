using System.ComponentModel.DataAnnotations;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Dtos;

// ---- Auth / Users ----
public record RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string FullName { get; init; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; init; } = "";
}

public record LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required] public string Password { get; init; } = "";
}

public record ChangeRoleRequest
{
    [Required] public UserRole? Role { get; init; }
}

public record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
public record UserResponse(Guid Id, string FullName, string Email, string Role, DateTime CreatedAt);

// ---- Projects ----
public record CreateProjectRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Name { get; init; } = "";
    [StringLength(1000)] public string? Description { get; init; }
}

public record UpdateProjectRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Name { get; init; } = "";
    [StringLength(1000)] public string? Description { get; init; }
}

public record ProjectResponse(
    Guid Id, string Name, string? Description, Guid OwnerId, string OwnerName,
    DateTime CreatedAt, int TotalTasks, int CompletedTasks);

// ---- Tasks ----
public record CreateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; init; } = "";
    [StringLength(2000)] public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public DateTime? DueDate { get; init; }
    public Guid? AssigneeId { get; init; }
}

public record UpdateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; init; } = "";
    [StringLength(2000)] public string? Description { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public DateTime? DueDate { get; init; }
}

public record ChangeStatusRequest
{
    [Required] public TaskItemStatus? Status { get; init; }
}

public record AssignTaskRequest
{
    /// null = quitar responsable
    public Guid? AssigneeId { get; init; }
}

public record TaskFilter(TaskItemStatus? Status, TaskPriority? Priority, Guid? AssigneeId);

public record TaskResponse(
    Guid Id, string Title, string? Description, TaskItemStatus Status, TaskPriority Priority,
    DateTime? DueDate, Guid ProjectId, Guid? AssigneeId, string? AssigneeName,
    DateTime CreatedAt, DateTime UpdatedAt);
