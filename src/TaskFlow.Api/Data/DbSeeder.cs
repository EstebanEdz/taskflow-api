using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Data;

public static class DbSeeder
{
    /// Crea un Admin inicial si la tabla de usuarios está vacía.
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var cfg = sp.GetRequiredService<IConfiguration>();
        var hasher = sp.GetRequiredService<IPasswordHasher<User>>();

        if (await db.Users.AnyAsync()) return;
        var email = cfg["Seed:AdminEmail"];
        var pass = cfg["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass)) return;

        var admin = new User { FullName = "Administrador", Email = email.ToLowerInvariant(), Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, pass);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
