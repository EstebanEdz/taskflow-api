using System.Security.Claims;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Common;

public interface ICurrentUser
{
    Guid Id { get; }
    UserRole Role { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new UnauthorizedException("No autenticado.");

    public Guid Id => Guid.Parse(Principal.FindFirstValue("sub")
        ?? throw new UnauthorizedException("Token sin identificador de usuario."));

    public UserRole Role => Enum.Parse<UserRole>(Principal.FindFirstValue("role")
        ?? throw new UnauthorizedException("Token sin rol."));
}
