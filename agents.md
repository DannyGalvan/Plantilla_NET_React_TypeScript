# Agentes - Plantilla NET 8 + React + TypeScript

## Descripción General

Este documento describe los agentes de desarrollo y herramientas disponibles para trabajar con esta plantilla de solución que combina un backend en .NET 8 con un frontend en React y TypeScript.

## Estructura del Proyecto

```
Plantilla_NET_React_TypeScript/
├── Project.Server/              # Backend en .NET 8
│   ├── Project.Server.csproj
│   ├── Controllers/
│   ├── Services/
│   └── Models/
├── project.client/              # Frontend en React + TypeScript
│   ├── src/
│   ├── tsconfig.json
│   ├── package.json
│   └── vite.config.ts
└── agents.md                     # Este archivo
```

## Agentes y Herramientas Disponibles

### 1. **Agente de Modernización .NET**
- **Descripción**: Asiste en la actualización y modernización del proyecto .NET 8
- **Casos de uso**:
  - Upgrade de versiones de .NET
  - Migración de componentes obsoletos a características modernas
  - Actualización de paquetes NuGet
  - Migración a Azure

### 2. **Agente de Perfiles de Rendimiento**
- **Descripción**: Herramienta para diagnósticos de rendimiento y perfilado
- **Casos de uso**:
  - Identificar cuellos de botella en el código
  - Optimizar performance del servidor
  - Análisis de memoria
  - Benchmarking

### 3. **Gestor de Paquetes NuGet**
- **Descripción**: Gestión de dependencias del backend
- **Casos de uso**:
  - Buscar versiones disponibles de paquetes
  - Obtener READMEs de paquetes
  - Actualizar paquetes a versiones compatibles
  - Resolver vulnerabilidades

### 4. **Búsqueda de Código**
- **Descripción**: Búsqueda semántica en el workspace
- **Casos de uso**:
  - Encontrar funcionalidades específicas
  - Localizar patrones de código
  - Búsqueda contextual

### 5. **Análisis de Símbolos**
- **Descripción**: Navegación y análisis de símbolos del código
- **Casos de uso**:
  - Ir a definición de clases/métodos
  - Encontrar todas las referencias
  - Ver implementaciones de interfaces
  - Rastrear uso de símbolos

### 6. **Compilación y Build**
- **Descripción**: Compilación del proyecto
- **Casos de uso**:
  - Validar cambios de código
  - Identificar errores de compilación
  - Verificar integridad de la solución

### 7. **Ejecución de Pruebas**
- **Descripción**: Ejecutar y descubrir tests
- **Casos de uso**:
  - Ejecutar suite de pruebas
  - Validar cambios
  - Descubrir tests por tipo, nombre o estado

### 8. **Documentación Microsoft Learn**
- **Descripción**: Acceso a documentación oficial de Microsoft
- **Casos de uso**:
  - Consultar documentación de .NET 8
  - Obtener ejemplos de código
  - Guías de implementación

## Stack Tecnológico

### Backend
- **.NET 8**: Framework principal del servidor
- **C#**: Lenguaje de programación
- **ASP.NET Core**: Framework web
- **NuGet**: Gestor de paquetes

### Frontend
- **React**: Biblioteca UI
- **TypeScript**: Lenguaje tipado para JavaScript
- **Vite**: Build tool y dev server
- **npm/yarn**: Gestor de paquetes

## Workflows Comunes

### Agregar una Nueva Dependencia
1. Buscar paquete con el agente de NuGet
2. Obtener información del README
3. Actualizar versión compatible
4. Ejecutar build para validar

### Optimizar Rendimiento
1. Usar agente de perfiles para medir
2. Identificar cuellos de botella
3. Implementar mejoras
4. Validar con perfiles nuevamente

### Modernizar Código
1. Activar agente de modernización
2. Identificar componentes obsoletos
3. Aplicar cambios sugeridos
4. Ejecutar tests para validar

### Investigar Código Existente
1. Usar búsqueda de código para conceptos
2. Usar análisis de símbolos para referencias
3. Verificar con compilación
4. Ejecutar tests relacionados

## Mejores Prácticas

- ✅ Siempre compilar después de cambios significativos
- ✅ Ejecutar pruebas antes de hacer commit
- ✅ Mantener dependencias actualizadas
- ✅ Documentar cambios importantes
- ✅ Usar análisis de código para verificar calidad

## Recursos

- [Documentación .NET 8](https://learn.microsoft.com/en-us/dotnet/)
- [React TypeScript](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)

## Nota

Este archivo se actualiza conforme se añadan nuevas herramientas y agentes al workspace.

# Especificaciones
- Existe un servicio generico para los crud llamado EntityService, el cual no necesita heredarse si no solo inyectarse, ya que se basa en el tipo de entidad que se le indique al inyectar, el tipo de su llave primaria y el request, por Ejemplo: 
```csharp para hacer el crud de usuarios necesitamos: 
   Clase de User que herede de IEntity<long> que es la interfaz que lleva las propiedades obligatorias de toda entidad de dominio y el tipo de dato de la llave primaria, en este caso long, esta clase llevaria todas las propiedades necesarias para crear un usuario, como por ejemplo: Nombre, Apellido, Email, etc.
   crear los Dtos necesarios para el crud de usuarios
   UserRequest, UserResponse
   UserRequest debe implementar IRequest<long> que lleva las propiedades obligatorias de toda entidad de dominio y el tipo de dato de la llave primaria, tendria todas las propieades necesarias para crear un usuario, todas las propiedades se crearian como nullable ya que esta misma clase se usaria tanto para crear como para actualizar un usuario, y en el caso de actualizar solo se enviarian las propiedades que se desean actualizar, mientras que UserResponse tendria todas las propiedades necesarias para mostrar la informacion de un usuario, incluyendo su id y cualquier otra propiedad que se desee mostrar.
   UserResponse se usaria para mostrar la informacion de un usuario, mientras que UserRequest se usaria para crear o actualizar un usuario
   crear los validadores para la clase, son necesarios y obligatorios 3, para crear el usuario, para actualizar el usuario, para actualizar parcialmente el usuario
   CreateUserValidator, UpdateUserValidator, PartialUserValidator
   CreateUserValidator siempre va a heredar de CreateValidator<UserRequest>, UpdateUserValidator siempre va a heredar de UpdateValidator<UserRequest>, PartialUserValidator siempre va a heredar de PartialUpdateValidator<UserRequest> que son validadores comunes
   que llevan la logica de toda propiedad comun como el id y las propiedades de auditoria de una entidad de bd
   CreateUserValidator se usaria para validar la informacion necesaria para crear un usuario, UpdateUserValidator se usaria para validar la informacion necesaria para actualizar un usuario, PartialUserValidator se usaria para validar la informacion necesaria para actualizar parcialmente un usuario
   las actualizaciones parciales se haran con el metodo Patch, y se usaria el validador PartialUserValidator para validar la informacion enviada, este validador se encargaria de validar solo las propiedades que se desean actualizar, mientras que las propiedades que no se desean actualizar no serian validadas, ya que son opcionales
   la forma correcta de inyectar las validaciones siempre sera en la configuracion correspondiente de inyeccion de dependencia dentro de configs/extensions/ValidationGroup.cs
   y como se inyectan los validadores de la siguiente manera:
   services.AddKeyedScoped<IValidator<UserRequest>, CreateUserValidator>("Create");
   services.AddKeyedScoped<IValidator<UserRequest>, UpdateUserValidator>("Update");
   services.AddKeyedScoped<IValidator<UserRequest>, PartialUserValidator>("Partial");
   adicional necesitamos crear nuestros mappers, para esto se utiliza la libreria mapster y mapstermapper para poder inyectarlos por medio de inyeccion de dependencia
   la forma correcta de crear los mapper para la IEntityService siempre va a ser 
   TypeAdapterConfig<RegisterRequest, User>.NewConfig() de request a user
   TypeAdapterConfig<User, UserResponse>.NewConfig() de user a response
   TypeAdapterConfig<User, User>.NewConfig(); esto es de user a user que sirve dentro de la logica de actualizacion para mapear la entidad con la informacion enviada en el request, y asi solo actualizar las propiedades que se desean actualiza
   EntityService se inyectaria de la siguiente manera:
   services.AddScoped<IEntityService<User,UserRequest,long>, EntityService<User,UserRequest,long>>();
   tambien existe un controlador generico llamado CrudController el cual para implementarse solamente se hereda y se le indica el tipo de entidad, el tipo de request, el tipo de response y el tipo de llave primaria, por ejemplo:
   [Route("api/[controller]")]
   public class UserController : CrudController<User, UserRequest, UserResponse, long>
   {
	   public UserController(IEntityService<User, UserRequest, long> service) : base(service)
	   {
	   }
   }
   a menos de que se requieran acciones personalizadas ahi se puede agregar la logica correspondiente y es valido, pero si solo se necesitan las acciones basicas de un crud no es necesario agregar nada mas, ya que el CrudController ya tiene implementadas las acciones basicas de un crud como GetAll, GetById, Create, Update, Delete y Patch para actualizaciones parciales
```
- siempre se debe de respetar la convencion para crear los servicios crud
- siempre que te dirigas a mi en el chat dirigete como Danny

## Sistema de Permisos con Sincronización Automática

### Descripción
El sistema de permisos utiliza reflexión para sincronizar automáticamente los módulos (controladores) y operaciones (acciones) con la base de datos al iniciar la aplicación. Esto elimina la necesidad de mantener seeds manuales extensos.

### Componentes Principales

#### 1. **Atributos Personalizados**

##### `[ModuleInfo]` - Para Controladores
Decorador para controladores que define metadata del módulo:
```csharp
[ModuleInfo(
	DisplayName = "Gestión de Usuarios",
	Description = "Módulo para administrar usuarios del sistema",
	Icon = "users",
	Path = "Users",
	Order = 1,
	IsVisible = true
)]
[Route("api/v1/[controller]")]
public class UserController : CrudController<User, UserRequest, UserResponse, long>
{
	// Las operaciones se sincronizan automáticamente
}
```

**Propiedades disponibles**:
- `DisplayName`: Nombre para mostrar en el frontend
- `Description`: Descripción del módulo
- `Icon`: Icono (Ionicons) para el UI
- `Path`: Ruta del frontend
- `Order`: Orden de aparición en menús
- `IsVisible`: Si debe mostrarse en navegación

##### `[OperationInfo]` - Para Acciones
Decorador para métodos que define metadata de la operación:
```csharp
[OperationInfo(
	DisplayName = "Exportar Usuarios",
	Description = "Exportar lista de usuarios a Excel",
	Icon = "download",
	Path = "Users/Export",
	IsVisible = true
)]
[HttpGet("export")]
public IActionResult Export()
{
	// Lógica de exportación
}
```

**Propiedades disponibles**:
- `DisplayName`: Nombre para mostrar
- `Description`: Descripción de la operación
- `Icon`: Icono para el UI
- `Path`: Ruta relativa
- `IsVisible`: Si debe aparecer en menús
- `ExcludeFromSync`: Excluir de sincronización

##### `[ExcludeFromSync]` - Exclusión de Sincronización
Marca controladores o acciones para excluirlos de la sincronización automática:
```csharp
[Route("api/v1/[controller]")]
public class AuthController : CommonController
{
	[ExcludeFromSync] // No requiere permiso - público
	[AllowAnonymous]
	[HttpPost]
	public ActionResult Login(LoginRequest model) { }
}
```

**Uso común**: Endpoints públicos como Login, Register que no deben tener restricciones de permisos.

#### 2. **Servicios de Sincronización**

##### `IOperationSyncService`
Interfaz para el servicio de sincronización:
```csharp
public interface IOperationSyncService
{
	Task SyncAsync(); // Sincroniza módulos y operaciones
	Task AssignAllOperationsToAdminRoleAsync(string roleName = "SA"); // Asigna permisos al rol admin
}
```

##### `OperationSyncService`
Implementación que:
1. Usa `IActionDescriptorCollectionProvider` para obtener todas las acciones
2. Filtra las que tienen `[ExcludeFromSync]`
3. Lee metadata de `[ModuleInfo]` y `[OperationInfo]`
4. Crea/Actualiza entidades `Module` y `Operation` en BD
5. Genera identificadores únicos (`OperationKey`: `Controller.Action.HttpMethod`)
6. Asigna automáticamente todas las operaciones al rol Administrador (SA)

##### `OperationSyncHostedService`
Servicio de background (`IHostedService`) que ejecuta la sincronización al iniciar la aplicación.

#### 3. **Entidades Extendidas**

##### Module
Campos adicionales:
- `Order` (int): Orden de visualización

##### Operation
Campos técnicos adicionales:
- `ControllerName` (string): Nombre del controlador
- `ActionName` (string): Nombre de la acción
- `HttpMethod` (string): GET, POST, PUT, DELETE, PATCH
- `RouteTemplate` (string): Template de ruta completo
- `OperationKey` (string): Identificador único (ej: "User.GetAll.GET")

### Flujo de Sincronización

```
Inicio App → OperationSyncHostedService
			  ↓
		SyncAsync()
			  ↓
  Descubrir controladores y acciones (Reflexión)
			  ↓
  Filtrar [ExcludeFromSync]
			  ↓
  Leer atributos [ModuleInfo] y [OperationInfo]
			  ↓
  Crear/Actualizar Module y Operation en BD
			  ↓
  AssignAllOperationsToAdminRoleAsync()
			  ↓
  Crear RolOperation para rol SA
			  ↓
  Sincronización Completada ✓
```

### Ventajas del Sistema

1. **Automatización**: No más seeds manuales de módulos/operaciones
2. **Sincronización**: Automáticamente detecta nuevos controladores/acciones
3. **Declarativo**: Metadata de UI definida con atributos en el código
4. **Idempotente**: Puede ejecutarse múltiples veces sin efectos secundarios
5. **Rol Admin**: Asignación automática de todos los permisos al rol SA
6. **Exclusión Flexible**: Permite excluir endpoints públicos fácilmente
7. **Trazabilidad**: Campos técnicos almacenan información completa de cada operación

### Ejemplo Completo

```csharp
// Controlador CRUD con decorador de módulo
[ModuleInfo(
	DisplayName = "Gestión de Productos",
	Description = "Módulo para administrar productos",
	Icon = "cube",
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

	// Las operaciones heredadas se sincronizan automáticamente:
	// - GetAll (GET)     → Products.GetAll
	// - Get (GET)        → Products.Get
	// - Create (POST)    → Products.Create
	// - Update (PUT)     → Products.Update
	// - PartialUpdate (PATCH) → Products.PartialUpdate
	// - Delete (DELETE)  → Products.Delete

	// Operación personalizada con metadata
	[OperationInfo(
		DisplayName = "Exportar Productos",
		Description = "Exportar lista de productos a Excel",
		Icon = "download",
		Path = "Products/Export",
		IsVisible = true
	)]
	[HttpGet("export")]
	[Authorize(Policy = "Products.Export")]
	public async Task<IActionResult> Export()
	{
		// Lógica de exportación
		return File(bytes, "application/vnd.ms-excel", "productos.xlsx");
	}
}

// Controlador con exclusiones
[Route("api/v1/[controller]")]
[ApiController]
public class AuthController : CommonController
{
	[ExcludeFromSync] // Público - no requiere permiso
	[AllowAnonymous]
	[HttpPost]
	public ActionResult Login(LoginRequest model)
	{
		// Lógica de login
	}

	[OperationInfo(DisplayName = "Resetear Contraseña")]
	[HttpPost("reset-password")]
	[Authorize] // Requiere autenticación - SÍ se sincroniza
	public ActionResult ResetPassword(ResetPasswordRequest model)
	{
		// Lógica de reset
	}
}
```

### Políticas de Autorización

Las operaciones generan automáticamente políticas en formato:
```
{ControllerName}.{ActionName}
```

Ejemplos:
- `Products.GetAll` - Listar productos
- `Products.Create` - Crear producto
- `Products.Export` - Exportar productos
- `Users.Delete` - Eliminar usuario

Estas políticas se usan con `[Authorize(Policy = "...")]` en los controladores.

### Mejores Prácticas

1. **Decorar controladores**: Siempre agregar `[ModuleInfo]` a nuevos controladores
2. **Operaciones públicas**: Marcar con `[ExcludeFromSync]` si no requieren autenticación
3. **Operaciones personalizadas**: Usar `[OperationInfo]` para metadata de UI
4. **Orden lógico**: Asignar `Order` coherente para agrupación en menús
5. **Iconos consistentes**: Usar iconos de Ionicons para uniformidad
6. **Descripciones claras**: Facilitan la asignación de permisos desde el UI

### Migración desde Seeds Manuales

Si tienes seeds existentes en `DataContext.cs`:
1. El sistema respetará GUIDs existentes
2. Actualizará campos faltantes (ControllerName, ActionName, etc.)
3. No eliminará operaciones existentes
4. Puedes limpiar los seeds de `Module`, `Operation` y `RolOperation`
5. Mantén solo seeds de `Rol` y `User` base

### Troubleshooting

**Problema**: Las operaciones no se sincronizan
- Verificar que `OperationSyncHostedService` esté registrado en DI
- Revisar logs de inicio de aplicación
- Confirmar que el controlador no tiene `[ExcludeFromSync]`

**Problema**: Operación marcada como excluida se sincronizó
- Verificar que el atributo esté en el método correcto
- Limpiar y recompilar solución
- Verificar que la base de datos se actualizó con la migración

**Problema**: No aparecen iconos o paths en el frontend
- Verificar que los atributos `[ModuleInfo]` y `[OperationInfo]` tengan los valores correctos
- Confirmar que el mapper de respuestas incluye los nuevos campos

### Referencias

- Ubicación de atributos: `Project.Server/Attributes/`
- Servicio de sincronización: `Project.Server/Services/Core/OperationSyncService.cs`
- Hosted service: `Project.Server/Services/Background/OperationSyncHostedService.cs`
- Configuración DI: `Project.Server/Configs/Extensions/ServicesGroup.cs`
- Plan detallado: `PLAN_MEJORA_PERMISOS_Y_REFLEXION.md`
