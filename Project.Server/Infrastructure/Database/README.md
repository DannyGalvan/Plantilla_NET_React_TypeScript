# Guía de uso: Soporte Multi-Base de Datos

Este proyecto está configurado para soportar tres proveedores de base de datos:
- **SQL Server**
- **PostgreSQL**
- **MySQL**

## Estructura de configuración

La configuración se divide en dos archivos:
1. `appsettings.json` - Configuración predeterminada
2. `appsettings.{Provider}.json` - Configuraciones específicas por proveedor

## Cambiar entre proveedores de base de datos

### Opción 1: Modificar `appsettings.json`

Actualiza la sección `Database` en `appsettings.json`:

```json
{
  "Database": {
    "Provider": "SqlServer",  // Cambiar a "PostgreSql" o "MySql"
    "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Opción 2: Usar archivos específicos por ambiente

Los archivos de configuración específicos están disponibles:

- **SQL Server**: `appsettings.SqlServer.json`
- **PostgreSQL**: `appsettings.PostgreSql.json`
- **MySQL**: `appsettings.MySql.json`

Para usar una configuración específica, renombra el archivo a `appsettings.{Environment}.json`:

```bash
# Para Development
copy appsettings.PostgreSql.json appsettings.Development.json

# Para Production
copy appsettings.MySql.json appsettings.Production.json
```

## Configuración de proveedores

### SQL Server

```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

**Conexión de ejemplo para servidor remoto:**
```
Server=SERVIDOR\INSTANCIA;Database=ProyectoDb;User Id=usuario;Password=contraseña;TrustServerCertificate=true;
```

### PostgreSQL

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Port=5432;Database=ProyectoDb;Username=postgres;Password=postgres;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

**Conexión de ejemplo para servidor remoto:**
```
Host=servidor.com;Port=5432;Database=ProyectoDb;Username=usuario;Password=contraseña;SSL Mode=Require;
```

### MySQL

```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Port=3306;Database=ProyectoDb;User=root;Password=root;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

**Conexión de ejemplo para servidor remoto:**
```
Server=servidor.com;Port=3306;Database=ProyectoDb;User=usuario;Password=contraseña;
```

## Opciones de configuración

| Opción | Tipo | Descripción | Valor por defecto |
|--------|------|-------------|-------------------|
| `Provider` | string | Proveedor de BD: `SqlServer`, `PostgreSql`, `MySql` | `SqlServer` |
| `ConnectionString` | string | Cadena de conexión a la base de datos | - |
| `EnableAutoMigration` | boolean | Aplicar migraciones automáticamente al iniciar | `false` |
| `EnableSensitiveDataLogging` | boolean | Registrar datos sensibles en logs | `false` |
| `EnableChangeTracking` | boolean | Habilitar seguimiento de cambios | `true` |

## Migraciones automáticas

Para habilitar migraciones automáticas:

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "...",
    "EnableAutoMigration": true
  }
}
```

⚠️ **ADVERTENCIA**: Solo habilitar en desarrollo. En producción, ejecuta migraciones manualmente:

```bash
dotnet ef database update
```

## Crear migraciones

Las migraciones funcionan igual que antes con todos los proveedores:

```bash
# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Actualizar base de datos
dotnet ef database update

# Revertir última migración
dotnet ef migrations remove
```

## Arquitectura interna

### Componentes principales

1. **DatabaseProvider** (Enum)
   - Define los proveedores soportados

2. **DatabaseSettings** (Clase)
   - Contiene la configuración de conexión

3. **IDatabaseConfigurator** (Interfaz)
   - Define la forma de configurar cada proveedor

4. **Configuradores concretos**
   - `SqlServerConfigurator`
   - `PostgreSqlConfigurator`
   - `MySqlConfigurator`

5. **DatabaseConfiguratorFactory** (Factory)
   - Selecciona el configurador según el proveedor

6. **DatabaseServiceExtensions** (Extensiones)
   - Registra los servicios en el contenedor de DI

## Flujo de inicialización

```
Program.cs
  ↓
AddContextGroup() en Configs.Extensions.ContextGroup
  ↓
AddMultiDatabaseSupport() en Infrastructure.Extensions.DatabaseServiceExtensions
  ↓
Leer configuración de "Database" en appsettings.json
  ↓
DatabaseConfiguratorFactory.GetConfigurator()
  ↓
Configurador específico (SqlServer/PostgreSQL/MySQL)
  ↓
DataContext registrado en DI
  ↓
ApplyMigrations() (si EnableAutoMigration = true)
```

## Ejemplo de uso en código

El cambio de base de datos es transparente para el código de aplicación:

```csharp
// En un servicio o controlador
public class UserService
{
    private readonly DataContext _context;

    public UserService(DataContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        // El mismo código funciona con cualquier proveedor
        return await _context.Users.ToListAsync();
    }
}
```

## Solución de problemas

### Problema: "No se puede conectar a la base de datos"
- Verificar que el servidor esté corriendo
- Validar la cadena de conexión
- Comprobar permisos de usuario

### Problema: "Proveedor no soportado"
- Asegurarse de que `Provider` esté correctamente escrito
- Los valores válidos son: `SqlServer`, `PostgreSql`, `MySql` (case-sensitive)

### Problema: "Migraciones no se aplican"
- Verificar que `EnableAutoMigration` sea `true`
- Revisar los logs para errores de migración
- Ejecutar manualmente: `dotnet ef database update`

## Mejores prácticas

✅ **DO:**
- Usar variables de entorno para cadenas de conexión en producción
- Mantener la configuración de conexión en `appsettings.{Environment}.json`
- Testear migraciones antes de aplicar en producción
- Usar `EnableAutoMigration` solo en desarrollo

❌ **DON'T:**
- Guardar contraseñas en el código
- Usar `EnableAutoMigration` en producción
- Habilitar `EnableSensitiveDataLogging` en producción
- Cambiar el proveedor sin revisar compatibilidad de datos

## Recursos adicionales

- [Entity Framework Core - Multiple Providers](https://docs.microsoft.com/en-us/ef/core/providers/)
- [SQL Server Provider](https://docs.microsoft.com/en-us/ef/core/providers/sql-server/)
- [PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [MySQL Provider](https://www.pomelo.io/efcore)
