using TaskFlow.Api.Common;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;
using static TaskFlow.Tests.TestHelpers;

namespace TaskFlow.Tests;

public class ProjectServiceTests
{
    [Fact]
    public async Task Create_AsMember_ThrowsForbidden()
    {
        using var db = NewDb();
        var member = AddUser(db);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new ProjectService(db, As(member)).CreateAsync(new CreateProjectRequest { Name = "Proyecto X" }));
    }

    [Fact]
    public async Task Create_AsManager_SetsOwnerAndReturnsCounts()
    {
        using var db = NewDb();
        var manager = AddUser(db, UserRole.Manager);
        var result = await new ProjectService(db, As(manager)).CreateAsync(new CreateProjectRequest { Name = "Proyecto X" });

        Assert.Equal(manager.Id, result.OwnerId);
        Assert.Equal(0, result.TotalTasks);
    }

    [Fact]
    public async Task Delete_WithPendingTasks_ThrowsConflict()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var project = AddProject(db, owner);
        AddTask(db, project, TaskItemStatus.InProgress);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ProjectService(db, As(owner)).DeleteAsync(project.Id));
    }

    [Fact]
    public async Task Delete_OnlyDoneTasks_Succeeds()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var project = AddProject(db, owner);
        AddTask(db, project, TaskItemStatus.Done);

        await new ProjectService(db, As(owner)).DeleteAsync(project.Id);
        Assert.Empty(db.Projects);
    }

    [Fact]
    public async Task Update_ByOtherManager_ThrowsForbidden()
    {
        using var db = NewDb();
        var owner = AddUser(db, UserRole.Manager);
        var other = AddUser(db, UserRole.Manager);
        var project = AddProject(db, owner);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new ProjectService(db, As(other)).UpdateAsync(project.Id, new UpdateProjectRequest { Name = "Hackeado" }));
    }
}
