using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, ITokenService tokens) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        // Todo registro público entra como Member; los roles superiores los asigna un Admin.
        var user = new User { FullName = request.FullName.Trim(), Email = email, Role = UserRole.Member };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mismo mensaje para "no existe" y "clave incorrecta": no revelar qué correos están registrados.
        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Credenciales inválidas.");

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User u)
    {
        var (token, exp) = tokens.Create(u);
        return new AuthResponse(token, exp, UserService.ToResponse(u));
    }
}
