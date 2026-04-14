# Sistema de Autorización JWT con Operaciones (RBAC)

## 📋 Descripción General

Este proyecto implementa un sistema de **autorización basada en operaciones (RBAC - Role-Based Access Control)** usando **JWT (JSON Web Tokens)** para una API REST segura y escalable.

## 🔒 Componentes de Seguridad

### 1. Autenticación JWT
- **Stateless**: Sin sesiones de servidor, ideal para APIs RESTful
- **Claims personalizados**: Incluye información del usuario y operaciones permitidas
- **Expiración configurable**: Control de tiempo de vida del token

### 2. Autorización Basada en Operaciones
- **Granularidad**: Control a nivel de Controller.Action.HttpMethod
- **Dinámico**: Las operaciones se asignan por rol en base de datos
- **Validación automática**: Usando ASP.NET Core Authorization Policies

---

## 🔧 Arquitectura

### Flujo de Autenticación

```
1. Usuario → POST /api/v1/Auth/Login → AuthService
2. AuthService valida credenciales
3. AuthService.GetToken() genera JWT con claims:
   - NameIdentifier: UserId
   - Email: Email del usuario
   - Name: Nombre completo
   - Operator: RolId
   - OperationKey: Todas las operaciones permitidas (ej: "User.Create.POST")
4. Cliente recibe JWT en response
5. Cliente incluye JWT en header: Authorization: Bearer {token}
```

### Flujo de Autorización

```
1. Request → Middleware JWT valida token
2. [Authorize] verifica que el usuario esté autenticado
3. [RequireOperation] ejecuta OperationAuthorizationHandler
4. Handler verifica si el usuario tiene el claim "OperationKey" requerido
5. Si tiene permiso → 200 OK
6. Si NO tiene permiso → 403 Forbidden
```

---

## 📝 Uso en Controladores

### Proteger un Controlador Completo

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Todos los endpoints requieren autenticación
public class UserController : CrudController<User, UserRequest, UserResponse, long>
{
    // ...
}
```

### Proteger Acciones Específicas

```csharp
[HttpGet]
[RequireOperation] // Valida automáticamente: User.GetAll.GET
public IActionResult GetAll([FromQuery] QueryParamsRequest query)
{
    return base.GetAll(query);
}

[HttpPost]
[RequireOperation] // Valida automáticamente: User.Create.POST
public IActionResult Create([FromBody] UserRequest request)
{
    return base.Create(request);
}
```

### Endpoints Públicos (Sin Autenticación)

```csharp
[HttpPost]
[AllowAnonymous] // Permite acceso sin JWT
public ActionResult Login(LoginRequest model)
{
    // Login público
}
```

---

## 🔑 Configuración de Operaciones

### 1. Estructura de Operaciones en Base de Datos

La tabla `Operation` almacena las operaciones disponibles:

```sql
CREATE TABLE Operation (
    Id BIGINT PRIMARY KEY,
    ControllerName VARCHAR(100),  -- "User"
    ActionName VARCHAR(100),       -- "Create"
    HttpMethod VARCHAR(10),        -- "POST"
    OperationKey VARCHAR(200),     -- "User.Create.POST"
    ...
)
```

### 2. Asignar Operaciones a Roles

La tabla `RolOperation` vincula roles con operaciones:

```sql
CREATE TABLE RolOperation (
    Id BIGINT PRIMARY KEY,
    RolId BIGINT,
    OperationId BIGINT,
    State INT  -- 1=Activo, 0=Inactivo
)
```

### 3. Claims en el JWT

Al hacer login, el JWT incluye claims `OperationKey`:

```json
{
  "nameid": "123",
  "email": "admin@example.com",
  "name": "Administrator",
  "Operator": "1",
  "OperationKey": [
    "User.Create.POST",
    "User.GetAll.GET",
    "User.Update.PUT",
    "Rol.Create.POST"
  ]
}
```

---

## 🛠️ Implementación Técnica

### OperationAuthorizationHandler

Valida que el usuario tenga el `OperationKey` requerido:

```csharp
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    OperationAuthorizationRequirement requirement)
{
    var operationKeyClaims = context.User.FindAll("OperationKey").ToList();
    
    var userOperationKeys = operationKeyClaims
        .Select(c => c.Value)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (userOperationKeys.Contains(requirement.OperationKey))
    {
        context.Succeed(requirement);
    }
    
    return Task.CompletedTask;
}
```

### RequireOperationAttribute

Atributo que construye automáticamente el OperationKey:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireOperationAttribute : TypeFilterAttribute
{
    // Construye: {Controller}.{Action}.{HttpMethod}
    // Ejemplo: "User.Create.POST"
}
```

---

## 📊 Ejemplo Completo

### 1. Crear un Nuevo Endpoint Protegido

```csharp
[HttpPatch("{id}/activate")]
[RequireOperation("User.Activate.PATCH")] // OperationKey explícito
public IActionResult ActivateUser(long id)
{
    // Solo usuarios con la operación "User.Activate.PATCH" pueden ejecutar esto
}
```

### 2. Agregar la Operación a la BD

```sql
INSERT INTO Operation (ControllerName, ActionName, HttpMethod, OperationKey, Name, ModuleId, State)
VALUES ('User', 'Activate', 'PATCH', 'User.Activate.PATCH', 'Activar Usuario', 2, 1);
```

### 3. Asignar al Rol Administrador

```sql
INSERT INTO RolOperation (RolId, OperationId, State)
VALUES (1, {OperationId}, 1);
```

### 4. Resultado

- ✅ Usuarios con rol Administrador (RolId=1) pueden ejecutar `PATCH /api/v1/User/{id}/activate`
- ❌ Otros usuarios reciben `403 Forbidden`

---

## 🚫 Errores Comunes

### 1. "401 Unauthorized"
**Causa**: JWT no enviado o inválido  
**Solución**: Verificar header `Authorization: Bearer {token}`

### 2. "403 Forbidden"
**Causa**: Usuario autenticado pero sin permiso para la operación  
**Solución**: 
- Verificar que la operación existe en BD
- Verificar que `RolOperation` asigna la operación al rol del usuario
- Verificar que el JWT contiene el claim `OperationKey` correcto

### 3. "OperationKey claim not found"
**Causa**: JWT no contiene las operaciones  
**Solución**: Verificar que `AuthService.GetToken()` incluye las operaciones en los claims

### 4. ❌ **CRÍTICO: Olvidar [RequireOperation] en un método**
**Causa**: Método sin `[RequireOperation]` permite acceso a cualquier usuario autenticado  
**Impacto de Seguridad**: 🔴 ALTO - Bypass de todo el sistema RBAC  
**Solución**: 
```csharp
// ❌ VULNERABLE
[HttpDelete("{id}")]
public IActionResult Delete(long id) { ... }

// ✅ SEGURO
[HttpDelete("{id}")]
[RequireOperation]
public IActionResult Delete(long id) { ... }
```

### 5. "Could not determine operation key for authorization"
**Causa**: Controller o Action name no pudo ser extraído de la ruta  
**Solución**: Verificar que el controlador hereda de `ControllerBase` o `Controller` y tiene `[ApiController]`

---

## 🔄 Migración desde SessionAuthorizationFilter

El `SessionAuthorizationFilter` (basado en sesiones HTTP) ha sido marcado como **obsoleto** y reemplazado por el sistema JWT + OperationAuthorizationHandler.

### Diferencias Clave

| Aspecto | SessionAuthorizationFilter (Obsoleto) | JWT + OperationAuthorizationHandler (Actual) |
|---------|----------------------------------------|----------------------------------------------|
| Estado | Stateful (sesiones de servidor) | Stateless (sin sesiones) |
| Escalabilidad | Limitada (sesiones en memoria) | Alta (sin estado compartido) |
| Multi-plataforma | No (solo navegadores con cookies) | Sí (APIs, móviles, SPAs) |
| Seguridad | Cookies HTTP | Bearer Token en headers |
| Validación | Manual en cada request | ASP.NET Core Authorization Policies |

---

## 📚 Referencias

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)

---

## 🎯 Resumen

✅ **Seguridad API REST moderna con JWT**  
✅ **Control granular de permisos por operación**  
✅ **Escalable y stateless**  
✅ **Compatible con frontend SPA, móviles, etc.**  
✅ **Fácil de mantener y extender**
