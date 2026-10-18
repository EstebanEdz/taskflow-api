# TaskFlow API
[![CI](https://github.com/EstebanEdz/taskflow-api/actions/workflows/ci.yml/badge.svg)](https://github.com/EstebanEdz/taskflow-api/actions/workflows/ci.yml)

API REST para gestión de proyectos y tareas. ASP.NET Core 8 · EF Core · SQL Server · JWT · xUnit.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download) o superior
- SQL Server (instancia local, Express o LocalDB)

## Puesta en marcha

```powershell
git clone https://github.com/EstebanEdz/taskflow-api
cd TaskFlow
```

**1. Cadena de conexión.** `appsettings.json` apunta a LocalDB (`(localdb)\MSSQLLocalDB`). Si usas otra instancia
(por ejemplo SQL Server instalado como `MSSQLSERVER`), copia la plantilla y ajústala:

```powershell
copy src\TaskFlow.Api\appsettings.Development.example.json src\TaskFlow.Api\appsettings.Development.json
```

Para una instancia con nombre usa `Server=localhost\\SQLEXPRESS` (doble barra en JSON).

**2. Ejecutar.** La migración ya está incluida; al arrancar en *Development* se crea la base `TaskFlowDb`
y el usuario administrador inicial.

```powershell
dotnet run --project src/TaskFlow.Api
```

El perfil de `launchSettings.json` activa el entorno *Development* y escucha en
`https://localhost:5001` y `http://localhost:5000`. Al abrir la raíz (`/`) se redirige a **Swagger** (`/swagger`).

> Si ejecutas con `--no-launch-profile`, define antes `$env:ASPNETCORE_ENVIRONMENT = "Development"`.
> Fuera de Development no se aplican migraciones, no se crea el admin y Swagger no se publica.

Primera vez con HTTPS: `dotnet dev-certs https --trust`.

**3. Probar.** En Swagger haz login en `POST /api/auth/login` con el admin inicial:

```json
{ "email": "admin@taskflow.local", "password": "12345678" }
```

Copia el `token`, pulsa **Authorize** y pégalo (sin la palabra `Bearer`).

## Pruebas automáticas

```powershell
dotnet test tests/TaskFlow.Tests
```

Usan una base de datos en memoria; no necesitan SQL Server.
(`dotnet test` desde la raíz también funciona con el SDK 9.0.200 o superior, que entiende `TaskFlow.slnx`.)

## Configuración y secretos

- `Jwt:Key` en `appsettings.json` es solo un ejemplo para desarrollo. Fuera de Development la API **no arranca**
  mientras conserve ese valor. Define el tuyo con `dotnet user-secrets` o con la variable de entorno `Jwt__Key`.
- `Seed:AdminEmail` / `Seed:AdminPassword` definen el admin inicial (solo se crea en Development y si no hay usuarios).

## Roles

| Rol | Puede |
|---|---|
| Member | Ver proyectos/tareas; cambiar estado de las tareas que tiene asignadas |
| Manager | Lo de Member + crear proyectos; gestiona sus proyectos y tareas |
| Admin | Todo, incluida la gestión de usuarios y roles |

El registro público crea siempre usuarios `Member`; un Admin sube el rol con `PUT /api/users/{id}/role`.
El rol viaja dentro del token: tras cambiarlo, el usuario debe volver a iniciar sesión.

## Endpoints

| Método | Ruta | Acceso |
|---|---|---|
| POST | /api/auth/register · /api/auth/login | Público |
| GET | /api/users/me | Autenticado |
| GET | /api/users | Admin |
| PUT | /api/users/{id}/role | Admin |
| POST / GET | /api/projects | Manager+ / Autenticado |
| GET / PUT / DELETE | /api/projects/{id} | Autenticado / Dueño o Admin |
| POST / GET | /api/projects/{id}/tasks | Dueño o Admin / Autenticado |
| GET | /api/tasks/mine · /api/tasks/{id} | Autenticado |
| PUT / DELETE | /api/tasks/{id} | Dueño del proyecto o Admin |
| PATCH | /api/tasks/{id}/status | Dueño, Admin o responsable |
| PATCH | /api/tasks/{id}/assignee | Dueño o Admin |

## Decisiones de diseño

- **Errores uniformes**: excepciones de dominio + `IExceptionHandler` → `ProblemDetails` (RFC 7807).
- **Consultas eficientes**: `AsNoTracking`, proyección directa a DTO, paginación e índices en `Email`, `(ProjectId, Status)` y `AssigneeId`.
- **Máquina de estados** para tareas (Todo → InProgress → InReview → Done, con reapertura).
- **Regla de borrado**: un proyecto con tareas sin terminar no se elimina (409).
- **Contraseñas** con `PasswordHasher<T>` (PBKDF2); el login no revela si el correo existe.
- Lógica en servicios (testeable sin HTTP); controladores delgados.

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| `404` al abrir `https://localhost:5001/` | Versiones antiguas no tenían página en la raíz. Usa `/swagger` o actualiza a esta versión. |
| Error 26 / "Unable to locate a Local Database Runtime" | No existe esa instancia de SQL Server. Ajusta `ConnectionStrings:Default` (ver paso 1). |
| `warn: Failed to determine the https port for redirect` | Solo un aviso: la API escucha únicamente en HTTP. Usa el perfil `https` o ignóralo. |
| Swagger no aparece | La API arrancó fuera de Development (ver nota del paso 2). |
