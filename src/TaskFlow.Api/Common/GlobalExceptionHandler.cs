using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Common;

/// Convierte cualquier excepción en una respuesta ProblemDetails (RFC 7807).
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        ProblemDetails problem;
        if (ex is AppException app)
        {
            problem = new() { Status = app.StatusCode, Title = app.Title, Detail = app.Message };
        }
        else
        {
            logger.LogError(ex, "Error no controlado en {Path}", ctx.Request.Path);
            problem = new() { Status = 500, Title = "Error interno", Detail = "Ocurrió un error inesperado." };
        }

        problem.Instance = ctx.Request.Path;
        ctx.Response.StatusCode = problem.Status!.Value;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
