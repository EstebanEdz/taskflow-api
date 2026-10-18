using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Common;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserService users) : ControllerBase
{
    /// <summary>Información del usuario autenticado.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) =>
        Ok(await users.GetMeAsync(ct));

    /// <summary>Lista usuarios paginados (solo Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<UserResponse>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await users.ListAsync(page, pageSize, ct));

    /// <summary>Cambia el rol de un usuario (solo Admin).</summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> ChangeRole(Guid id, ChangeRoleRequest request, CancellationToken ct) =>
        Ok(await users.ChangeRoleAsync(id, request.Role!.Value, ct));
}
