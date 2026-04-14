# 🔧 Guía de Troubleshooting - Multi-Base de Datos

## Problemas comunes y soluciones

---

## ❌ "Your target project doesn't reference EntityFramework" o "Both EF6 and EF Core are installed"

### Causa
Estás usando el comando de Entity Framework 6 en lugar de Entity Framework Core

### Solución

❌ **NO usar**:
```powershell
update-database
```

✅ **SÍ usar** (dotnet CLI):
```bash
dotnet ef database update
```

✅ **O en Package Manager Console**:
```powershell
EntityFrameworkCore\Update-Database
```

### Comandos correctos para EF Core:

| Acción | dotnet CLI | Package Manager Console |
|--------|-----------|------------------------|
| Crear migración | `dotnet ef migrations add Nombre` | `EntityFrameworkCore\Add-Migration Nombre` |
| Aplicar migraciones | `dotnet ef database update` | `EntityFrameworkCore\Update-Database` |
| Ver migraciones | `dotnet ef migrations list` | `EntityFrameworkCore\Get-Migration` |
| Remover migración | `dotnet ef migrations remove` | `EntityFrameworkCore\Remove-Migration` |

### Si no tienes dotnet-ef instalado:
```bash
dotnet tool install --global dotnet-ef
```

O actualizar:
```bash
dotnet tool update --global dotnet-ef
```

---

## ❌ "The connection string 'DefaultConnection' was not found"

### Causa
No se encontró la cadena de conexión en appsettings.json

### Solución
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;",
    "EnableAutoMigration": false
  }
}
```

✅ Verificar que el JSON sea válido (sin comas faltantes)

---

## ❌ "An instance of type 'DataContext' cannot be created"

### Causa
El DbContext no está registrado correctamente en Dependency Injection

### Solución
Verificar que en `Configs/Extensions/ContextGroup.cs` esté:
```csharp
public static IServiceCollection AddContextGroup(this IServiceCollection services, IConfiguration configuration)
{
    services.AddMultiDatabaseSupport(configuration);
    return services;
}
```

✅ Y que en `Program.cs` se llame a `AddContextGroup(configuration)`

---

## ❌ "Cannot connect to database server"

### Causa
El servidor de base de datos no está corriendo o la cadena de conexión es incorrecta

### Solución

#### Para SQL Server:
```bash
# Verificar que SQL Server esté corriendo
sc query mssqlserver

# O iniciar LocalDB
sqllocaldb start mssqllocaldb
```

#### Para PostgreSQL:
```bash
# Verificar conexión
psql -h localhost -U postgres -d postgres

# Reiniciar servicio (Linux)
sudo systemctl restart postgresql
```

#### Para MySQL:
```bash
# Verificar conexión
mysql -h localhost -u root -p

# Reiniciar servicio (Linux)
sudo systemctl restart mysql
```

---

## ❌ "Database provider 'XXX' is not supported"

### Causa
El nombre del provider en appsettings.json está mal escrito o no es reconocido

### Solución
Los valores válidos son (case-sensitive):
- `SqlServer` ✅
- `PostgreSql` ✅
- `MySql` ✅

❌ No usar:
- `sqlserver` (minúsculas)
- `sql_server` (con guion)
- `sqlserver2019` (con versión)

```json
{
  "Database": {
    "Provider": "PostgreSql"  // ✅ Correcto
  }
}
```

---

## ❌ "The property doesn't have a setter" (Migraciones)

### Causa
La configuración de migraciones no está correctamente aplicada

### Solución
Ejecutar:
```bash
# Eliminar última migración
dotnet ef migrations remove

# Crear nueva migración
dotnet ef migrations add InitialCreate

# Aplicar
dotnet ef database update
```

---

## ❌ "Unable to resolve service for type 'IConfiguration'"

### Causa
Falta el using de Microsoft.Extensions.Configuration en algún archivo

### Solución
En `Infrastructure/Extensions/DatabaseServiceExtensions.cs` verificar:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
```

---

## ❌ "No such table: __EFMigrationsHistory"

### Causa
La tabla de historial de migraciones no existe (base de datos nueva)

### Solución
```bash
# Aplicar migraciones
dotnet ef database update
```

Esto crea automáticamente la tabla `__EFMigrationsHistory`

---

## ❌ "Timeout expired. The timeout period elapsed..."

### Causa
La base de datos tarda mucho tiempo en responder (red lenta, servidor sobrecargado)

### Solución

#### Aumentar timeout en la cadena de conexión:

**SQL Server:**
```
Connection Timeout=60;
```

**PostgreSQL:**
```
Timeout=60;
```

**MySQL:**
```
Connection Timeout=60;
```

---

## ❌ "Access denied for user 'usuario'@'localhost'"

### Causa
Contraseña incorrecta o usuario sin permisos

### Solución

#### Para PostgreSQL:
```bash
# Conectarse con postgres (administrador)
psql -U postgres

# Resetear contraseña
ALTER USER proyecto WITH PASSWORD 'nueva_contraseña';
```

#### Para MySQL:
```bash
# Conectarse con root
mysql -u root -p

# Resetear contraseña
ALTER USER 'proyecto'@'localhost' IDENTIFIED BY 'nueva_contraseña';
FLUSH PRIVILEGES;
```

---

## ❌ "Unknown database 'proyecto_db'"

### Causa
La base de datos no existe

### Solución

#### Para PostgreSQL:
```bash
psql -U postgres

# Dentro de psql
CREATE DATABASE proyecto_db;
```

#### Para MySQL:
```bash
mysql -u root -p

# Dentro de mysql
CREATE DATABASE proyecto_db;
```

---

## ❌ "The specified model type is not a valid type for this DbSet"

### Causa
El DbSet no está registrado correctamente en DataContext

### Solución
En `Context/DataContext.cs` verificar que exista:
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Module> Modules { get; set; }
// ... todos los DbSets necesarios
```

---

## ❌ "There are pending migrations"

### Causa
Hay migraciones creadas pero no aplicadas

### Solución
```bash
# Ver migraciones pendientes
dotnet ef migrations list

# Aplicar todas
dotnet ef database update

# O ver qué cambios hace
dotnet ef migrations script
```

---

## ❌ "Npgsql.NpgsqlException: no database exists"

### Causa
La base de datos PostgreSQL especificada no existe

### Solución
```bash
# Conectarse a postgres (BD por defecto)
psql -h localhost -U postgres -d postgres

# Crear base de datos
CREATE DATABASE proyecto_db OWNER proyecto;
```

---

## ⚠️ Migraciones fallaron - Revertir cambios

### Opción 1: Revertir última migración
```bash
dotnet ef migrations remove
dotnet ef database update <migracion-anterior>
```

### Opción 2: Revertir a base de datos limpia
```bash
# Eliminar todas las migraciones
dotnet ef database update 0

# O si ya está limpia
DROP DATABASE proyecto_db;
CREATE DATABASE proyecto_db;

# Crear migraciones nuevas
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 🔍 Debug y Diagnóstico

### Ver información del contexto
```bash
dotnet ef dbcontext info
```

### Ver todas las migraciones
```bash
dotnet ef migrations list
```

### Generar script SQL sin ejecutar
```bash
dotnet ef migrations script > script.sql
```

### Habilitar logs detallados
En `appsettings.json`:
```json
{
  "Database": {
    "EnableSensitiveDataLogging": true
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

---

## ✅ Verificación rápida de salud

### Verificar que todo está correctamente configurado:

```csharp
// En un endpoint de health check
[HttpGet("health/database")]
public async Task<IActionResult> DatabaseHealth()
{
    try
    {
        await _context.Database.ExecuteSqlAsync($"SELECT 1");
        return Ok(new { status = "healthy", database = "connected" });
    }
    catch (Exception ex)
    {
        return StatusCode(503, new { status = "unhealthy", error = ex.Message });
    }
}
```

---

## 📝 Checklist antes de cambiar de BD

- [ ] Backup de datos actuales
- [ ] Probar cadena de conexión localmente
- [ ] Verificar que el servidor esté corriendo
- [ ] Verificar permisos de usuario
- [ ] Actualizar appsettings.json
- [ ] Ejecutar migraciones: `dotnet ef database update`
- [ ] Testear la aplicación
- [ ] Verificar integridad de datos

---

## 🆘 Si nada funciona

### Paso nuclear (Restart completo)
```bash
# 1. Detener la aplicación
# 2. Eliminar base de datos
DROP DATABASE proyecto_db;

# 3. Recrear base de datos
CREATE DATABASE proyecto_db;

# 4. Eliminar migraciones locales
rm -r Migrations/  # Linux/Mac
rmdir /S Migrations  # Windows

# 5. Crear migración nueva
dotnet ef migrations add InitialCreate

# 6. Aplicar migraciones
dotnet ef database update

# 7. Iniciar aplicación
dotnet run
```

### Contactar soporte
Si el problema persiste:
1. Recolectar logs: `dotnet run > logs.txt 2>&1`
2. Incluir versión de BD: `SELECT version();`
3. Incluir appsettings.json (sin contraseñas)
4. Incluir stack trace del error

---

## 📚 Comandos útiles

```bash
# Migraciones
dotnet ef migrations add NombreMigracion
dotnet ef migrations list
dotnet ef migrations remove
dotnet ef database update
dotnet ef database drop

# Diagnóstico
dotnet ef dbcontext info
dotnet ef migrations script

# Limpiar
dotnet clean
dotnet build
```

---

## 💡 Tips de prevención

✅ **DO:**
- Hacer backup antes de cambiar de BD
- Testear migraciones en desarrollo primero
- Usar `EnableAutoMigration: false` en producción
- Documentar cambios de BD

❌ **DON'T:**
- Cambiar provider sin preparación
- Eliminar migraciones antiguas
- Guardar contraseñas en código
- Usar `EnableAutoMigration` en producción

---

**¿Problema no listado aquí? Revisa:**
- Los logs: Output window de Visual Studio
- Event Viewer (Windows): Event logs del sistema
- System logs (Linux): `sudo journalctl -u mysql`
