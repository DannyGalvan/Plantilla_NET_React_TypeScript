# Plan de Mejora: Sistema de Permisos con Reflexión

## 📊 Estado Actual del Sistema

### Entidades Existentes

#### 1. **Module** (`Entities/Models/Module.cs`)
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | long | Identificador único |
| Name | string | Nombre del módulo |
| Description | string | Descripción |
| Image | string | Icono del módulo |
| Path | string | Ruta del frontend |
| State | int | Estado (activo/inactivo) |
| CreatedAt | DateTime | Fecha de creación |
| UpdatedAt | DateTime? | Fecha de actualización |
| CreatedBy | long | Usuario creador |
| UpdatedBy | long? | Usuario actualizador |
| Operations | ICollection | Operaciones relacionadas |

#### 2. **Operation** (`Entities/Models/Operation.cs`)
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Id | long | Identificador único |
| Guid | string | Identificador único global |
| Name | string | Nombre de la operación |
| Description | string | Descripción |
| Policy | string | Política de autorización (ej: "Payments.List") |
| Icon | string | Icono para el frontend |
| Path | string | Ruta del frontend |
| ModuleId | long | FK al módulo |
| IsVisible | bool | Si es visible en menú |
| State | int | Estado |
| + Campos de auditoría |

#### 3. **Rol** (`Entities/Models/Rol.cs`)
- Id, Name, Description, State, campos de auditoría
- Relación con Users y RolOperations

#### 4. **RolOperation** (`Entities/Models/RolOperation.cs`)
- Tabla puente entre Rol y Operation
- Id, RolId, OperationId, State, campos de auditoría

### Problema Actual
1. **Todo se hace manual** en `DataContext.cs` con `HasData()` (Seeds)
2. Los módulos y operaciones se definen manualmente (~400+ líneas de seeds)
3. Las RolOperations para el rol SA se deben agregar manualmente
4. **No hay sincronización automática** cuando se agregan nuevos controladores o acciones
5. La información de UI (Icon, Path, IsVisible, Description) debe definirse en código separado

### CrudController Base - Acciones Disponibles
```csharp
[HttpGet]      GetAll()        // Listar
[HttpGet]      Get(id)         // Obtener por ID
[HttpPost]     Create()        // Crear
[HttpPut]      Update()        // Actualizar
[HttpPatch]    PartialUpdate() // Actualización parcial
[HttpDelete]   Delete(id)      // Eliminar
```

---

## 🎯 Objetivo de la Mejora

Implementar un sistema de **sincronización automática** de módulos y operaciones usando **reflexión** que:

1. **Detecte automáticamente** todos los controladores (= Módulos)
2. **Detecte automáticamente** todas las acciones (= Operaciones)
3. Use **atributos personalizados** para definir metadata de UI
4. **Sincronice con la BD** al iniciar la aplicación
5. **Asigne automáticamente** todas las operaciones al rol Administrador (SA)
6. Permita **excluir** controladores/acciones específicas (ej: AuthController.Login)

---

## 📋 Plan de Implementación

### Fase 1: Crear Atributos Personalizados

#### Paso 1.1: Crear `ModuleInfoAttribute`
**Archivo:** `Project.Server/Attributes/ModuleInfoAttribute.cs`
```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ModuleInfoAttribute : Attribute
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "folder";
    public string Path { get; set; } = string.Empty;
    public int Order { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
}
```

#### Paso 1.2: Crear `OperationInfoAttribute`
**Archivo:** `Project.Server/Attributes/OperationInfoAttribute.cs`
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OperationInfoAttribute : Attribute
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "circle";
    public string Path { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool ExcludeFromSync { get; set; } = false;
}
```

#### Paso 1.3: Crear `ExcludeFromSyncAttribute`
**Archivo:** `Project.Server/Attributes/ExcludeFromSyncAttribute.cs`
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ExcludeFromSyncAttribute : Attribute
{
}
```

### Fase 2: Modificar Entidades

#### Paso 2.1: Agregar campos a Operation
**Archivo:** `Project.Server/Entities/Models/Operation.cs`

Agregar nuevos campos para almacenar información técnica:
```csharp
public string ControllerName { get; set; } = string.Empty;
public string ActionName { get; set; } = string.Empty;
public string HttpMethod { get; set; } = string.Empty;
public string RouteTemplate { get; set; } = string.Empty;
public string OperationKey { get; set; } = string.Empty; // Ej: "User.GetAll.GET"
```

#### Paso 2.2: Agregar campo Order a Module
**Archivo:** `Project.Server/Entities/Models/Module.cs`
```csharp
public int Order { get; set; } = 0;
```

### Fase 3: Crear el Servicio de Sincronización

#### Paso 3.1: Crear Interfaz
**Archivo:** `Project.Server/Services/Interfaces/IOperationSyncService.cs`
```csharp
public interface IOperationSyncService
{
    Task SyncAsync();
    Task AssignAllOperationsToAdminRoleAsync(string roleName = "SA");
}
```

#### Paso 3.2: Crear Implementación
**Archivo:** `Project.Server/Services/Core/OperationSyncService.cs`

El servicio debe:
1. Usar `IActionDescriptorCollectionProvider` para obtener todas las acciones
2. Filtrar las que tienen `[ExcludeFromSync]`
3. Leer metadata de `[ModuleInfo]` y `[OperationInfo]`
4. Crear/Actualizar Módulos y Operaciones en BD
5. Generar GUID único si no existe
6. Construir `OperationKey` como: `{Controller}.{Action}.{HttpMethod}`
7. Construir `Policy` como: `{Controller}.{Action}`

### Fase 4: Crear HostedService para Sincronización al Inicio

#### Paso 4.1: Crear HostedService
**Archivo:** `Project.Server/Services/Background/OperationSyncHostedService.cs`
```csharp
public class OperationSyncHostedService : IHostedService
{
    // Ejecuta sincronización al iniciar la aplicación
}
```

### Fase 5: Configurar Inyección de Dependencias

#### Paso 5.1: Actualizar ServicesGroup.cs
**Archivo:** `Project.Server/Configs/Extensions/ServicesGroup.cs`
```csharp
services.AddScoped<IOperationSyncService, OperationSyncService>();
services.AddHostedService<OperationSyncHostedService>();
```

### Fase 6: Decorar Controladores con Atributos

#### Paso 6.1: Ejemplo en CrudController (heredados)
```csharp
[ModuleInfo(
    DisplayName = "Usuarios",
    Description = "Gestión de Usuarios",
    Icon = "users",
    Path = "Users",
    Order = 1
)]
[Route("api/v1/[controller]")]
public class UserController : CrudController<User, UserRequest, UserResponse, long>
{
    // Las operaciones heredan metadata por defecto del CrudController
}
```

#### Paso 6.2: Ejemplo en AuthController
```csharp
[ExcludeFromSync] // O por método específico
[Route("api/v1/[controller]")]
public class AuthController : CommonController
{
    [ExcludeFromSync] // Login no necesita permiso
    [HttpPost]
    public ActionResult Login(LoginRequest model) { }
    
    [OperationInfo(DisplayName = "Registrar Usuario", Icon = "user-plus")]
    [HttpPost("Register")]
    public ActionResult Register(RegisterRequest model) { }
}
```

### Fase 7: Migración de Base de Datos

#### Paso 7.1: Crear Migración
```bash
dotnet ef migrations add AddOperationSyncFields
dotnet ef database update
```

### Fase 8: Limpiar Seeds Manuales (Opcional)

#### Paso 8.1: Remover HasData de DataContext
- Mantener solo el seed del Rol SA y User Admin
- Remover seeds de Modules, Operations y RolOperations
- La sincronización automática los creará

---

## 🗂️ Estructura de Archivos a Crear

```
Project.Server/
├── Attributes/                           # NUEVO
│   ├── ModuleInfoAttribute.cs
│   ├── OperationInfoAttribute.cs
│   └── ExcludeFromSyncAttribute.cs
├── Services/
│   ├── Interfaces/
│   │   └── IOperationSyncService.cs      # NUEVO
│   ├── Core/
│   │   └── OperationSyncService.cs       # NUEVO
│   └── Background/                       # NUEVO
│       └── OperationSyncHostedService.cs
├── Entities/
│   └── Models/
│       ├── Module.cs                     # MODIFICAR (agregar Order)
│       └── Operation.cs                  # MODIFICAR (agregar campos técnicos)
├── Configs/
│   └── Extensions/
│       └── ServicesGroup.cs              # MODIFICAR (registrar servicios)
└── Context/
    └── DataContext.cs                    # MODIFICAR (limpiar seeds opcionales)
```

---

## 📝 Checklist de Implementación

### Fase 1: Atributos
- [ ] Crear carpeta `Attributes`
- [ ] Crear `ModuleInfoAttribute.cs`
- [ ] Crear `OperationInfoAttribute.cs`
- [ ] Crear `ExcludeFromSyncAttribute.cs`

### Fase 2: Entidades
- [ ] Modificar `Module.cs` - agregar campo `Order`
- [ ] Modificar `Operation.cs` - agregar campos técnicos

### Fase 3: Servicios
- [ ] Crear `IOperationSyncService.cs`
- [ ] Crear `OperationSyncService.cs`
- [ ] Crear carpeta `Background`
- [ ] Crear `OperationSyncHostedService.cs`

### Fase 4: Configuración
- [ ] Actualizar `ServicesGroup.cs`

### Fase 5: Controladores
- [ ] Decorar `AuthController` con atributos de exclusión
- [ ] Crear ejemplo de controlador decorado

### Fase 6: Base de Datos
- [ ] Crear migración EF Core
- [ ] Aplicar migración

### Fase 7: Pruebas
- [ ] Verificar sincronización al inicio
- [ ] Verificar asignación automática a rol SA
- [ ] Verificar exclusión de acciones específicas

---

## 🔄 Flujo de Sincronización

```
┌─────────────────────┐
│  Inicio Aplicación  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────────────────┐
│  OperationSyncHostedService     │
│  ejecuta SyncAsync()            │
└──────────┬──────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────┐
│  1. Obtener todos los ActionDescriptors         │
│  2. Filtrar los que tienen [ExcludeFromSync]    │
│  3. Agrupar por Controller (= Módulos)          │
└──────────┬──────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────┐
│  Para cada Controller:                          │
│  - Leer [ModuleInfo] o usar defaults            │
│  - Crear/Actualizar Module en BD                │
└──────────┬──────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────┐
│  Para cada Action:                              │
│  - Leer [OperationInfo] o usar defaults         │
│  - Generar OperationKey único                   │
│  - Generar Policy                               │
│  - Crear/Actualizar Operation en BD             │
└──────────┬──────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────┐
│  AssignAllOperationsToAdminRoleAsync("SA")      │
│  - Obtener todas las Operations activas         │
│  - Crear RolOperation si no existe              │
└──────────┬──────────────────────────────────────┘
           │
           ▼
┌─────────────────────┐
│  Sincronización     │
│  Completada ✓       │
└─────────────────────┘
```

---

## 🎨 Ejemplo de Uso Final

### Controlador Decorado
```csharp
[ModuleInfo(
    DisplayName = "Gestión de Productos",
    Description = "Módulo para administrar productos del sistema",
    Icon = "box",
    Path = "Products",
    Order = 10,
    IsVisible = true
)]
[Route("api/v1/[controller]")]
public class ProductController : CrudController<Product, ProductRequest, ProductResponse, long>
{
    public ProductController(
        IEntityService<Product, ProductRequest, long> service, 
        IMapper mapper
    ) : base(service, mapper) { }

    // Heredará automáticamente las operaciones del CrudController:
    // - GetAll (GET)     -> Products.GetAll
    // - Get (GET)        -> Products.Get
    // - Create (POST)    -> Products.Create
    // - Update (PUT)     -> Products.Update
    // - PartialUpdate (PATCH) -> Products.PartialUpdate
    // - Delete (DELETE)  -> Products.Delete

    [OperationInfo(
        DisplayName = "Exportar Productos",
        Description = "Exportar lista de productos a Excel",
        Icon = "file-excel",
        Path = "Products/Export",
        IsVisible = true
    )]
    [HttpGet("export")]
    public IActionResult Export()
    {
        // Lógica de exportación
    }
}
```

### Controlador Excluido
```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class AuthController : CommonController
{
    [ExcludeFromSync] // No requiere permiso
    [AllowAnonymous]
    [HttpPost]
    public ActionResult Login(LoginRequest model) { }
    
    [ExcludeFromSync] // No requiere permiso
    [AllowAnonymous]
    [HttpPost("Register")]
    public ActionResult Register(RegisterRequest model) { }
}
```

---

## 📌 Notas Importantes

1. **Compatibilidad hacia atrás**: Los módulos/operaciones existentes se actualizarán, no se eliminarán
2. **GUID estable**: Si una operación ya tiene GUID, se mantiene
3. **Estado inicial**: Nuevos módulos/operaciones se crean con `State = 1` (activo)
4. **Auditoría**: CreatedBy = 1 (sistema) para sincronización automática
5. **Orden de ejecución**: La sincronización corre ANTES de que la app acepte requests
6. **Idempotente**: Puede ejecutarse múltiples veces sin efectos secundarios

---

## 🚀 Próximos Pasos

1. **Confirmar plan** - ¿Hay algún ajuste necesario?
2. **Implementar Fase 1** - Crear los atributos
3. **Implementar Fase 2** - Modificar entidades
4. **Implementar Fase 3** - Crear servicio de sincronización
5. **Continuar con las demás fases**

---

*Documento generado el: $(Get-Date -Format "yyyy-MM-dd")*
*Versión: 1.0*
*Autor: GitHub Copilot para Danny*
