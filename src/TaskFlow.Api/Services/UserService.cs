using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Common;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Services;

public interface IUserService
{
    Task<UserResponse> GetMeAsync(CancellationToken ct = default);
    Task<PagedResult<UserResponse>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<UserResponse> ChangeRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
}

public class UserService(AppDbContext db, ICurrentUser current) : IUserService
{
    public static UserResponse ToResponse(User u) =>
        new(u.Id, u.FullName, u.Email, u.Role.ToString(), u.CreatedAt);

    public async Task<UserResponse> GetMeAsync(CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == current.Id, ct)
                   ?? throw new NotFoundException("Usuario no encontrado.");
        return ToResponse(user);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<UserResponse>.Normalize(page, pageSize);
        var query = db.Users.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<UserResponse>(items, page, pageSize, total);
    }

    public async Task<UserResponse> ChangeRoleAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        if (userId == current.Id)
            throw new BadRequestException("No puedes cambiar tu propio rol.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("Usuario no encontrado.");
        user.Role = role;
        await db.SaveChangesAsync(ct);
        return ToResponse(user);
    }
}
