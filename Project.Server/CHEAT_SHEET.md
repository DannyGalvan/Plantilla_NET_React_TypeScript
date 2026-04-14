┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│             🎯 CHEAT SHEET - CAMBIAR BASE DE DATOS                    │
│                                                                         │
│                        EN SOLO 3 PASOS ⚡                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ PASO 1️⃣  EDITAR appsettings.json                                      ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

Abre: Project.Server/appsettings.json

┌─────────────────────────────────────────────────────────────────────────┐
│ OPCION A: SQL SERVER (LocalDB)                                          │
├─────────────────────────────────────────────────────────────────────────┤
│ {                                                                       │
│   "Database": {                                                         │
│     "Provider": "SqlServer",                                           │
│     "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;        │
│                          Initial Catalog=ProyectoDb;                  │
│                          Integrated Security=true;                    │
│                          TrustServerCertificate=true;",               │
│     "EnableAutoMigration": true                                        │
│   }                                                                     │
│ }                                                                       │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ OPCION B: POSTGRESQL                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│ {                                                                       │
│   "Database": {                                                         │
│     "Provider": "PostgreSql",                                          │
│     "ConnectionString": "Host=localhost;Port=5432;                   │
│                          Database=proyecto_db;                        │
│                          Username=postgres;Password=postgres;",       │
│     "EnableAutoMigration": true                                        │
│   }                                                                     │
│ }                                                                       │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ OPCION C: MYSQL                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│ {                                                                       │
│   "Database": {                                                         │
│     "Provider": "MySql",                                               │
│     "ConnectionString": "Server=localhost;Port=3306;                 │
│                          Database=proyecto_db;                        │
│                          User=root;Password=root;",                   │
│     "EnableAutoMigration": true                                        │
│   }                                                                     │
│ }                                                                       │
└─────────────────────────────────────────────────────────────────────────┘

⚠️  IMPORTANTE:
    • "Provider" debe ser uno de: SqlServer, PostgreSql, MySql
    • ConnectionString debe ser válida para tu BD
    • EnableAutoMigration: true (en desarrollo)


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ PASO 2️⃣  EJECUTAR MIGRACION                                           ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

En terminal (dentro de Project.Server):

    $ dotnet ef database update

Espera a que termine...

✅ Si ves: "Successfully applied migration..."
   → ¡LISTO! Tu BD está lista


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ PASO 3️⃣  ¡LISTO!                                                      ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

Tu aplicación ahora usa la base de datos seleccionada.

    $ dotnet run

✅ La aplicación inicia con tu nueva BD
✅ SIN cambios en código
✅ SIN recompilar
✅ SIN breaking changes


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 🆘 SOLUCIONAR PROBLEMAS                                               ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

❌ "Cannot connect to database"
   → Verificar que BD server está corriendo
   → Verificar cadena de conexión
   → Ver TROUBLESHOOTING.md

❌ "Unknown database"
   → Crear la BD primero
   → Ver SETUP_DATABASE.md

❌ "Invalid provider"
   → Valores válidos: SqlServer, PostgreSql, MySql
   → Verificar que está correctamente escrito

❌ "Migrations failed"
   → Ejecutar: dotnet ef migrations remove
   → Luego: dotnet ef database update


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📚 DOCUMENTACION RAPIDA                                               ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

INDEX.md                      → Índice de toda la documentación
QUICK_REFERENCE.md            → Esta guía
SETUP_DATABASE.md             → Cómo instalar BD
CONFIGURATION_EXAMPLES.md     → Más ejemplos de config
TROUBLESHOOTING.md            → Solucionar problemas
ARQUITECTURA.md               → Entender el diseño


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 🔗 CADENAS DE CONEXION (COPIAR Y PEGAR)                               ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

SQL SERVER - LocalDB:
────────────────────
Data Source=(localdb)\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;


SQL SERVER - Remoto:
────────────────────
Server=IP_O_DOMINIO;Database=ProyectoDb;User Id=usuario;Password=contraseña;TrustServerCertificate=true;


POSTGRESQL - Local:
───────────────────
Host=localhost;Port=5432;Database=proyecto_db;Username=postgres;Password=postgres;


POSTGRESQL - Remoto:
────────────────────
Host=servidor.com;Port=5432;Database=proyecto_db;Username=usuario;Password=contraseña;SSL Mode=Require;


MYSQL - Local:
──────────────
Server=localhost;Port=3306;Database=proyecto_db;User=root;Password=root;


MYSQL - Remoto:
───────────────
Server=servidor.com;Port=3306;Database=proyecto_db;User=usuario;Password=contraseña;


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📋 TABLA DE REFERENCIA RAPIDA                                         ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

┌──────────┬────────┬──────────┬─────────────┬──────────────────────────┐
│ BD       │ Puerto │ Usuario  │ Contraseña  │ Provider para config     │
├──────────┼────────┼──────────┼─────────────┼──────────────────────────┤
│ SQL Srv  │ 1433   │ (Windows)│ -           │ "SqlServer"              │
│ PostgreS │ 5432   │ postgres │ postgres    │ "PostgreSql"             │
│ MySQL    │ 3306   │ root     │ root        │ "MySql"                  │
└──────────┴────────┴──────────┴─────────────┴──────────────────────────┘


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ✅ CHECKLIST ANTES DE CAMBIAR                                         ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

□ BD server está corriendo/instalado
□ Cadena de conexión es correcta
□ Usuario tiene permisos en BD
□ Nombre de BD existe
□ Provider está escrito correctamente
□ JSON en appsettings es válido
□ EnableAutoMigration está true (desarrollo)


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 🎯 COMANDOS UTILES                                                    ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

# Aplicar migraciones
dotnet ef database update

# Ver migraciones
dotnet ef migrations list

# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Revertir última migración
dotnet ef migrations remove

# Ver información del contexto
dotnet ef dbcontext info

# Limpiar y recompilar
dotnet clean
dotnet build

# Ejecutar aplicación
dotnet run


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 💡 TIPS PROFESIONALES                                                 ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

✨ Docker para BD locales rápidamente
   $ docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:15

✨ Usar variables de entorno en producción
   export DB_PROVIDER=PostgreSql
   export DB_CONNECTION_STRING="Host=prod;..."

✨ Testear antes de cambiar en producción
   Siempre prueba en desarrollo/staging primero

✨ Hacer backup antes de cambiar
   Especialmente si hay datos importantes

✨ Documentar cambios de BD
   Ayuda al equipo a saber qué BD se usa


┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ⚡ RESUMEN SUPER RAPIDO (1 MINUTO)                                    ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

1. Edita appsettings.json (cambia Provider y ConnectionString)
2. Ejecuta: dotnet ef database update
3. ¡Listo! Tu BD cambió

═════════════════════════════════════════════════════════════════════════════

¿MÁS AYUDA?

📚 Documentación: INDEX.md
🔍 Problemas: TROUBLESHOOTING.md
⚙️  Configuración: CONFIGURATION_EXAMPLES.md
🏗️  Arquitectura: ARQUITECTURA.md

═════════════════════════════════════════════════════════════════════════════
