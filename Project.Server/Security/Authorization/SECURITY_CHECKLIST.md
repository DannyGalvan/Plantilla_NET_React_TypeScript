# 🔒 Checklist de Seguridad RBAC

## ✅ Lista de Verificación para Nuevos Controladores

Al crear o modificar un controlador, asegúrate de cumplir con todos estos puntos:

### 1. Atributos a Nivel de Controlador

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // ✅ Requerido para todos los controladores protegidos
[ModuleInfo(...)]
public class MiController : CrudController<...>
```

- [ ] `[ApiController]` presente
- [ ] `[Authorize]` presente (excepto AuthController)
- [ ] `[ModuleInfo]` configurado correctamente

---

### 2. Atributos a Nivel de Método

**CADA método público DEBE tener uno de estos:**

#### Métodos Protegidos (mayoría)
```csharp
[HttpGet]
[RequireOperation] // ✅ OBLIGATORIO
[OperationInfo(...)]
public IActionResult GetAll([FromQuery] QueryParamsRequest query)
```

- [ ] `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpPatch]` / `[HttpDelete]`
- [ ] `[RequireOperation]` presente
- [ ] `[OperationInfo]` configurado

#### Métodos Públicos (solo Login/Register)
```csharp
[HttpPost]
[AllowAnonymous] // ✅ Solo para endpoints públicos
public ActionResult Login(LoginRequest model)
```

- [ ] `[AllowAnonymous]` solo en Login, Register y endpoints documentados como públicos

---

### 3. Operaciones en Base de Datos

Para cada método con `[RequireOperation]`, verifica:

- [ ] Existe registro en tabla `Operation` con:
  - `ControllerName` = Nombre del controlador (ej: "User")
  - `ActionName` = Nombre del método (ej: "Create")
  - `HttpMethod` = Verbo HTTP (ej: "POST")
  - `OperationKey` = "Controller.Action.HttpMethod" (ej: "User.Create.POST")
  - `State` = 1 (activo)

---

### 4. Permisos Asignados a Roles

- [ ] Existe registro en tabla `RolOperation` vinculando:
  - `RolId` del rol que debe tener acceso
  - `OperationId` de la operación creada
  - `State` = 1 (activo)

---

## 🔍 Auditoría de Controladores Existentes

### UserController
- [x] `[Authorize]` a nivel de clase
- [x] `GetAll` tiene `[RequireOperation]`
- [x] `Get` tiene `[RequireOperation]`
- [x] `Create` tiene `[RequireOperation]`
- [x] `Update` tiene `[RequireOperation]`
- [x] `PartialUpdate` tiene `[RequireOperation]`
- [x] `Delete` tiene `[RequireOperation]`

### RolController
- [x] `[Authorize]` a nivel de clase
- [x] `GetAll` tiene `[RequireOperation]`
- [x] `Get` tiene `[RequireOperation]`
- [x] `Create` tiene `[RequireOperation]`
- [x] `Update` tiene `[RequireOperation]`
- [x] `PartialUpdate` tiene `[RequireOperation]`
- [x] `Delete` tiene `[RequireOperation]`

### OperationController
- [x] `[Authorize]` a nivel de clase
- [x] `GetAll` tiene `[RequireOperation]`
- [x] `Get` tiene `[RequireOperation]`
- [x] `Create` tiene `[RequireOperation]`
- [x] `Update` tiene `[RequireOperation]`
- [x] `PartialUpdate` tiene `[RequireOperation]`
- [x] `Delete` tiene `[RequireOperation]`

### RolOperationController
- [x] `[Authorize]` a nivel de clase
- [x] `GetAll` tiene `[RequireOperation]`
- [x] `Get` tiene `[RequireOperation]`
- [x] `Create` tiene `[RequireOperation]`
- [x] `Update` tiene `[RequireOperation]`
- [x] `PartialUpdate` tiene `[RequireOperation]`
- [x] `Delete` tiene `[RequireOperation]`

### AuthController
- [x] NO tiene `[Authorize]` a nivel de clase (correcto, es público)
- [x] `Login` tiene `[AllowAnonymous]`
- [x] `Register` tiene `[AllowAnonymous]`

---

## 🚨 Alertas de Seguridad

### ⚠️ ANTES de hacer commit/push:

```bash
# 1. Buscar controladores sin [Authorize]
# (Excluyendo AuthController que es público)
grep -r "public class.*Controller" --include="*Controller.cs" | grep -v "Authorize"

# 2. Buscar métodos HTTP sin [RequireOperation] o [AllowAnonymous]
# (Todos los métodos públicos deben tener uno de estos)
grep -A 2 "\[Http" Controllers/*.cs | grep -v "RequireOperation\|AllowAnonymous"
```

### 🔴 NUNCA hacer esto:

```csharp
// ❌ Controlador sin [Authorize]
public class UserController : CrudController<...> 
{
    // Cualquier usuario puede acceder
}

// ❌ Método sin [RequireOperation] o [AllowAnonymous]
[HttpDelete("{id}")]
public IActionResult Delete(long id)
{
    // Cualquier usuario autenticado puede eliminar
}

// ❌ [AllowAnonymous] en operaciones sensibles
[HttpDelete("{id}")]
[AllowAnonymous] // ¡Peligro! Operación pública
public IActionResult Delete(long id)
{
    // Cualquier persona (incluso sin login) puede eliminar
}
```

### ✅ SIEMPRE hacer esto:

```csharp
// ✅ Controlador protegido
[Authorize]
public class UserController : CrudController<...>
{
    // ✅ Métodos con validación granular
    [HttpGet]
    [RequireOperation]
    public IActionResult GetAll() { ... }
    
    [HttpPost]
    [RequireOperation]
    public IActionResult Create() { ... }
}
```

---

## 📋 Template para Nuevos Controladores

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Attributes;
using Project.Server.Security.Authorization;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // ✅ OBLIGATORIO
    [ModuleInfo(
        DisplayName = "NombreModulo",
        Description = "Descripción del módulo",
        Icon = "bi-icon",
        Path = "ruta",
        Order = 10,
        IsVisible = true
    )]
    public class MiNuevoController : CrudController<Entidad, Request, Response, TId>
    {
        public MiNuevoController(
            IEntityService<Entidad, Request, TId> service,
            IMapper mapper) : base(service, mapper)
        {
        }

        [HttpGet]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(
            DisplayName = "Listar Items",
            Description = "Obtiene lista de items",
            Icon = "bi-list",
            Path = "ruta",
            IsVisible = true
        )]
        public override IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            return base.GetAll(query);
        }

        [HttpGet("{id}")]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(...)]
        public override IActionResult Get(TId id, string? include = null)
        {
            return base.Get(id, include);
        }

        [HttpPost]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(...)]
        public override IActionResult Create([FromBody] Request request)
        {
            return base.Create(request);
        }

        [HttpPut]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(...)]
        public override IActionResult Update([FromBody] Request request)
        {
            return base.Update(request);
        }

        [HttpPatch]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(...)]
        public override IActionResult PartialUpdate([FromBody] Request request)
        {
            return base.PartialUpdate(request);
        }

        [HttpDelete("{id}")]
        [RequireOperation] // ✅ OBLIGATORIO
        [OperationInfo(...)]
        public override IActionResult Delete(TId id)
        {
            return base.Delete(id);
        }
    }
}
```

---

## 🧪 Pruebas de Seguridad

### Caso 1: Usuario sin JWT
```bash
curl -X GET https://localhost:5001/api/v1/User
# Esperado: 401 Unauthorized
```

### Caso 2: Usuario con JWT pero sin permiso
```bash
curl -X DELETE https://localhost:5001/api/v1/User/1 \
  -H "Authorization: Bearer {token_sin_permiso_delete}"
# Esperado: 403 Forbidden
# Response: {"success":false,"message":"No tiene permisos para realizar esta operación"}
```

### Caso 3: Usuario con JWT y con permiso
```bash
curl -X DELETE https://localhost:5001/api/v1/User/1 \
  -H "Authorization: Bearer {token_con_permiso_delete}"
# Esperado: 200 OK
# Response: {"success":true,"message":"Entity User deleted successfully"}
```

---

## 📚 Referencias Rápidas

- **README principal**: `Project.Server\Security\Authorization\README.md`
- **Handler de autorización**: `Project.Server\Security\Authorization\OperationAuthorizationHandler.cs`
- **Atributo RequireOperation**: `Project.Server\Security\Authorization\RequireOperationAttribute.cs`
- **Generación de JWT**: `Project.Server\Services\Core\AuthService.cs` (método `GetToken()`)
- **Configuración de autorización**: `Project.Server\Configs\Extensions\AuthorizationConfiguration.cs`

---

## 🎯 Resumen de Reglas de Oro

1. ✅ **TODOS los controladores** (excepto AuthController) deben tener `[Authorize]`
2. ✅ **TODOS los métodos HTTP** deben tener `[RequireOperation]` O `[AllowAnonymous]`
3. ✅ **NUNCA** usar `[AllowAnonymous]` en operaciones que modifican datos
4. ✅ **SIEMPRE** sincronizar operaciones en BD con `OperationSyncService`
5. ✅ **SIEMPRE** asignar operaciones a roles vía `RolOperation`
6. ✅ **VERIFICAR** que el JWT contiene los claims `OperationKey` correctos
7. ✅ **PROBAR** manualmente con Postman/Swagger antes de deploy
