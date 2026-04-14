# 🏗️ Arquitectura Visual - Multi-Base de Datos

## Flujo de inicialización

```
┌─────────────────────────────────────────────────────────────────┐
│                         Program.cs                              │
│                  CreateBuilder(args)                            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              Cargar appsettings.json                            │
│  (appsettings.{Environment}.json si existe)                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│         builder.Services.AddContextGroup(configuration)         │
│       Configs/Extensions/ContextGroup.cs                        │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│    services.AddMultiDatabaseSupport(configuration)              │
│ Infrastructure/Extensions/DatabaseServiceExtensions.cs          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│         Leer sección "Database" de configuración                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ "Provider": "PostgreSql",                               │   │
│  │ "ConnectionString": "Host=localhost;...",              │   │
│  │ "EnableAutoMigration": true                            │   │
│  └─────────────────────────────────────────────────────────┘   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│         DatabaseConfiguratorFactory                             │
│         .GetConfigurator(settings.Provider)                     │
└────────┬────────────────┬────────────────┬────────────────┘
         │                │                │
         ▼                ▼                ▼
    ┌────────┐      ┌──────────┐     ┌──────────┐
    │ SqlSrv │      │ PostgreSQL│    │  MySQL   │
    │Config  │      │ Config    │    │ Config   │
    └────────┘      └──────────┘     └──────────┘
         │                │                │
         └────────┬───────┴────────┬───────┘
                  │                │
                  ▼                ▼
        DbContextOptionsBuilder configured
        
                  │
                  ▼
    ┌──────────────────────────────────────┐
    │    Registrar DataContext en DI       │
    │   (Ahora sabe qué provider usar)     │
    └──────────────────────────────────────┘
                  │
                  ▼
    ┌──────────────────────────────────────┐
    │  app.ApplyMigrations(configuration)  │
    │  Si EnableAutoMigration = true:      │
    │  context.Database.Migrate()          │
    └──────────────────────────────────────┘
                  │
                  ▼
    ┌──────────────────────────────────────┐
    │   ✨ Aplicación lista para usar      │
    └──────────────────────────────────────┘
```

---

## Inyección de Dependencias

```
┌─────────────────────────────────────────────────┐
│          Contenedor de DI                       │
├─────────────────────────────────────────────────┤
│                                                 │
│  IDataContext (DataContext)                    │
│  └─> Configurado con provider dinámico        │
│                                                 │
│  IOptions<DatabaseSettings>                    │
│  └─> Configuración de conexión                │
│                                                 │
│  IDatabaseConfigurator (Factory)               │
│  └─> Selecciona por DatabaseProvider           │
│                                                 │
│  [Otros servicios...]                          │
│  └─> Inyectan DataContext como antes           │
│                                                 │
└─────────────────────────────────────────────────┘
         ▲
         │
         └─ Transparente para servicios existentes
```

---

## Estructura de clases

```
┌─────────────────────────────────────┐
│      DatabaseProvider (Enum)        │
├─────────────────────────────────────┤
│  • SqlServer = 0                    │
│  • PostgreSql = 1                   │
│  • MySql = 2                        │
└─────────────────────────────────────┘

┌──────────────────────────────────────────┐
│      DatabaseSettings (Config)           │
├──────────────────────────────────────────┤
│  + Provider: DatabaseProvider            │
│  + ConnectionString: string              │
│  + EnableAutoMigration: bool             │
│  + EnableSensitiveDataLogging: bool      │
│  + EnableChangeTracking: bool            │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│   IDatabaseConfigurator (Interface)      │
├──────────────────────────────────────────┤
│  + Configure(options, connString)        │
│    : DbContextOptionsBuilder             │
└──────────────────────────────────────────┘
         △
         │ implements
         ├─────────┬──────────┬──────────┐
         │         │          │          │
    ┌────┴──┐ ┌────┴──┐ ┌────┴──┐       │
    │ SqlSrv│ │PgSql  │ │MySql  │       │
    │Config │ │Config │ │Config │       │
    └────┬──┘ └────┬──┘ └────┬──┘       │
         │         │          │          │
         └─────────┼──────────┘          │
                   │                     │
    ┌──────────────┴──────────────┐      │
    │ DatabaseConfiguratorFactory│      │
    ├───────────────────────────┤      │
    │ + GetConfigurator(Provider)│      │
    │   : IDatabaseConfigurator  │      │
    └───────────────────────────┘      │
```

---

## Flujo de datos en consulta

```
Servicio/Controlador
        │
        ▼
┌──────────────────────────┐
│    DataContext           │
│ (Inyectado vía DI)       │
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ DbContextOptions         │
│ (Configurado por Factory)│
└──────────────────────────┘
        │
        ├─ Proveedor: PostgreSQL
        ├─ Conexión: localhost:5432
        └─ Base datos: proyecto_db
        │
        ▼
┌──────────────────────────┐
│ Entity Framework Core    │
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ npgsql (PostgreSQL)      │
│ (O SqlClient/MySQLConn)  │
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ PostgreSQL Server        │
│ (O SQL Server/MySQL)     │
└──────────────────────────┘
```

---

## Archivo appsettings.json

```json
{
  "Logging": { ... },
  "AllowedHosts": "*",
  
  "Database": {                    ← Nueva sección
    "Provider": "PostgreSql",      ← Selecciona proveedor
    "ConnectionString": "...",     ← Cadena de conexión
    "EnableAutoMigration": true,   ← Migraciones automáticas
    "EnableSensitiveDataLogging": false,  ← Logging
    "EnableChangeTracking": true   ← Tracking
  }
}
```

---

## Cambio de proveedor

```
┌─────────────────────────────────────────────────────┐
│  Archivo appsettings.json ANTES                     │
├─────────────────────────────────────────────────────┤
│  "Provider": "SqlServer"                            │
│  "ConnectionString": "Data Source=..."             │
└─────────────────────────────────────────────────────┘
                        │
                        │ EDITAR
                        ▼
┌─────────────────────────────────────────────────────┐
│  Archivo appsettings.json DESPUÉS                   │
├─────────────────────────────────────────────────────┤
│  "Provider": "PostgreSql"                           │
│  "ConnectionString": "Host=localhost;..."          │
└─────────────────────────────────────────────────────┘
                        │
                        │ Ejecutar
                        ▼
              dotnet ef database update
                        │
                        ▼
    Base de datos migrada a PostgreSQL
                        │
                        ▼
          Aplicación usa PostgreSQL
                    (SIN CAMBIOS)
```

---

## Carpetas del proyecto

```
Project.Server/
│
├── Context/
│   └── DataContext.cs (✏️ Modificado)
│
├── Configs/
│   └── Extensions/
│       └── ContextGroup.cs (✏️ Modificado)
│
├── Infrastructure/ (✨ NUEVA)
│   ├── Database/
│   │   ├── Configurators/
│   │   │   ├── SqlServerConfigurator.cs (✨)
│   │   │   ├── PostgreSqlConfigurator.cs (✨)
│   │   │   └── MySqlConfigurator.cs (✨)
│   │   ├── DatabaseProvider.cs (✨)
│   │   ├── DatabaseSettings.cs (✨)
│   │   ├── IDatabaseConfigurator.cs (✨)
│   │   ├── DatabaseConfiguratorFactory.cs (✨)
│   │   └── README.md (✨)
│   └── Extensions/
│       └── DatabaseServiceExtensions.cs (✨)
│
├── Program.cs (✏️ Modificado)
├── appsettings.json (✏️ Modificado)
├── appsettings.SqlServer.json (✨)
├── appsettings.PostgreSql.json (✨)
├── appsettings.MySql.json (✨)
│
├── CAMBIOS.md (✨)
├── QUICK_REFERENCE.md (✨)
├── SETUP_DATABASE.md (✨)
├── CONFIGURATION_EXAMPLES.md (✨)
├── TROUBLESHOOTING.md (✨)
├── MULTI_DATABASE_SUMMARY.md (✨)
│
└── [Otros archivos del proyecto]

Legend:
✨ = Creado nuevo
✏️ = Modificado
```

---

## Compatibilidad de versiones

```
┌────────────────────────────────┐
│  .NET 8.0                      │
├────────────────────────────────┤
│  ✅ Microsoft.EntityFrameworkCore 9.0.5
│  ✅ Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10
│  ✅ Pomelo.EntityFrameworkCore.MySql 8.0.2
└────────────────────────────────┘
```

---

## Matriz de compatibilidad

```
┌──────────┬──────────┬──────────┬────────────┐
│ Provider │  Version │   Tested │   Status   │
├──────────┼──────────┼──────────┼────────────┤
│ SqlSrv   │   9.0.5  │   ✅     │    ✅ OK   │
│ PgSQL    │   8.0.10 │   ✅     │    ✅ OK   │
│ MySQL    │   8.0.2  │   ✅     │    ✅ OK   │
└──────────┴──────────┴──────────┴────────────┘
```

---

## Ciclo de vida de una solicitud

```
1. Solicitud HTTP llega
          │
          ▼
2. Se invoca Controlador/Servicio
          │
          ▼
3. Solicita DataContext (vía DI)
          │
          ▼
4. DI entrega DataContext
   (ya configurado con proveedor correcto)
          │
          ▼
5. Servicio ejecuta query
          │
          ▼
6. EF Core traduce a SQL específico del proveedor
   ├─ SQL Server: T-SQL
   ├─ PostgreSQL: PostgreSQL SQL
   └─ MySQL: MySQL SQL
          │
          ▼
7. Proveedor ejecuta en base de datos
          │
          ▼
8. Respuesta enviada
          │
          ▼
✅ Cliente recibe respuesta
   (Sin saber qué BD se usó)
```

---

## Abstracción del proveedor

```
                   Código de aplicación
                           │
                           ▼
                      DataContext
                      (Agnóstico)
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
   DbContextOptions    DbContextOptions   DbContextOptions
   (SqlServer)         (PostgreSQL)       (MySQL)
        │                  │                  │
        ▼                  ▼                  ▼
   SQL Server         PostgreSQL            MySQL
   (Específico)       (Específico)          (Específico)
```

---

**Diagrama de la arquitectura implementada ✨**
