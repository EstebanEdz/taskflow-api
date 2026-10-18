using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;

namespace TaskFlow.Tests;

public class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
}

public static class TestHelpers
{
    public static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public static User AddUser(AppDbContext db, UserRole role = UserRole.Member)
    {
        var u = new User { FullName = "Usuario Test", Email = $"{Guid.NewGuid():N}@t.com", Role = role };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    public static Project AddProject(AppDbContext db, User owner)
    {
        var p = new Project { Name = "Proyecto test", OwnerId = owner.Id };
        db.Projects.Add(p);
        db.SaveChanges();
        return p;
    }

    public static TaskItem AddTask(AppDbContext db, Project p, TaskItemStatus status = TaskItemStatus.Todo, Guid? assignee = null)
    {
        var t = new TaskItem { Title = "Tarea test", ProjectId = p.Id, Status = status, AssigneeId = assignee };
        db.Tasks.Add(t);
        db.SaveChanges();
        return t;
    }

    public static FakeCurrentUser As(User u) => new() { Id = u.Id, Role = u.Role };
}
