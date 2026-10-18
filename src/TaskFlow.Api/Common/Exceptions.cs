namespace TaskFlow.Api.Common;

public abstract class AppException(string message, int statusCode, string title) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

public class BadRequestException(string message) : AppException(message, 400, "Solicitud inválida");
public class UnauthorizedException(string message) : AppException(message, 401, "No autenticado");
public class ForbiddenException(string message = "No tienes permisos para esta acción.") : AppException(message, 403, "Acceso denegado");
public class NotFoundException(string message) : AppException(message, 404, "No encontrado");
public class ConflictException(string message) : AppException(message, 409, "Conflicto");
