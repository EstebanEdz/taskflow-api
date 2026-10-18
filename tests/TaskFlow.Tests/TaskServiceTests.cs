using TaskFlow.Api.Common;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;
using static TaskFlow.Tests.TestHelpers;

namespace TaskFlow.Tests;

public class TaskServiceTests
{
    [Fact]
    public async Task Create_ByNonOwnerMember_ThrowsForbidden()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var intruder = AddUser(db);
        var project = AddProject(db, owner);

        var svc = new TaskService(db, As(intruder));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.CreateAsync(project.Id, new CreateTaskRequest { Title = "Nueva tarea" }));
    }

    [Fact]
    public async Task Create_WithPastDueDate_ThrowsBadRequest()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var project = AddProject(db, owner);

        var svc = new TaskService(db, As(owner));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.CreateAsync(project.Id, new CreateTaskRequest { Title = "Vieja", DueDate = DateTime.UtcNow.AddDays(-3) }));
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_ThrowsBadRequest()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var task = AddTask(db, AddProject(db, owner)); // Todo

        var svc = new TaskService(db, As(owner));
        await Assert.ThrowsAsync<BadRequestException>(() => svc.ChangeStatusAsync(task.Id, TaskItemStatus.Done));
    }

    [Fact]
    public async Task ChangeStatus_ByAssignee_ValidTransition_Works()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var dev = AddUser(db);
        var task = AddTask(db, AddProject(db, owner), assignee: dev.Id);

        var result = await new TaskService(db, As(dev)).ChangeStatusAsync(task.Id, TaskItemStatus.InProgress);
        Assert.Equal(TaskItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task ChangeStatus_ByUnrelatedUser_ThrowsForbidden()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var stranger = AddUser(db);
        var task = AddTask(db, AddProject(db, owner));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new TaskService(db, As(stranger)).ChangeStatusAsync(task.Id, TaskItemStatus.InProgress));
    }

    [Fact]
    public async Task Assign_NonExistingUser_ThrowsBadRequest()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var task = AddTask(db, AddProject(db, owner));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            new TaskService(db, As(owner)).AssignAsync(task.Id, Guid.NewGuid()));
    }
}
