using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Tests;

public class AuthServiceTests
{
    private static AuthService Create(AppDbContext db) =>
        new(db, new PasswordHasher<User>(), new TokenService(Options.Create(new JwtSettings
        {
            Key = new string('k', 40), Issuer = "test", Audience = "test", ExpiresMinutes = 60
        })));

    [Fact]
    public async Task Register_CreatesMember_AndReturnsToken()
    {
        using var db = TestHelpers.NewDb();
        var result = await Create(db).RegisterAsync(new RegisterRequest
            { FullName = "Esteban", Email = "Test@Mail.com", Password = "Password123" });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("Member", result.User.Role);
        Assert.Equal("test@mail.com", result.User.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        using var db = TestHelpers.NewDb();
        var svc = Create(db);
        var req = new RegisterRequest { FullName = "Esteban", Email = "a@a.com", Password = "Password123" };
        await svc.RegisterAsync(req);

        await Assert.ThrowsAsync<ConflictException>(() => svc.RegisterAsync(req));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        using var db = TestHelpers.NewDb();
        var svc = Create(db);
        await svc.RegisterAsync(new RegisterRequest { FullName = "Esteban", Email = "a@a.com", Password = "Password123" });

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.LoginAsync(new LoginRequest { Email = "a@a.com", Password = "otra-clave" }));
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsToken()
    {
        using var db = TestHelpers.NewDb();
        var svc = Create(db);
        await svc.RegisterAsync(new RegisterRequest { FullName = "Esteban", Email = "a@a.com", Password = "Password123" });

        var result = await svc.LoginAsync(new LoginRequest { Email = "A@a.com", Password = "Password123" });
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }
}
