# Instrucciones de Instalación y Configuración por Base de Datos

## SQL Server (Predeterminado)

### Instalación

1. **Instalar SQL Server Express** (o usar LocalDB que ya viene con Visual Studio)
   - [Descargar SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
   - O usar LocalDB que ya está en Visual Studio

2. **Crear la base de datos**
   ```sql
   CREATE DATABASE ProyectoDb;
   GO
   ```

3. **Configurar la conexión en appsettings.json**
   ```json
   {
     "Database": {
       "Provider": "SqlServer",
       "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;",
       "EnableAutoMigration": true
     }
   }
   ```

4. **Aplicar migraciones**
   ```bash
   dotnet ef database update
   ```

### Conexión Remota (Servidor)
```json
{
  "ConnectionString": "Server=IP_SERVIDOR;Database=ProyectoDb;User Id=usuario;Password=contraseña;TrustServerCertificate=true;"
}
```

---

## PostgreSQL

### Instalación

1. **Instalar PostgreSQL**
   - [Descargar PostgreSQL](https://www.postgresql.org/download/)
   - **Windows**: Usar el instalador
   - **Linux**: `sudo apt-get install postgresql postgresql-contrib`
   - **macOS**: `brew install postgresql`

2. **Crear usuario y base de datos**
   ```bash
   # Conectarse a PostgreSQL
   psql -U postgres
   
   # Crear usuario (contraseña: postgres)
   CREATE USER proyecto WITH PASSWORD 'postgres';
   
   # Crear base de datos
   CREATE DATABASE proyecto_db OWNER proyecto;
   
   # Dar permisos
   GRANT ALL PRIVILEGES ON DATABASE proyecto_db TO proyecto;
   
   # Salir
   \q
   ```

3. **Configurar la conexión en appsettings.json**
   ```json
   {
     "Database": {
       "Provider": "PostgreSql",
       "ConnectionString": "Host=localhost;Port=5432;Database=proyecto_db;Username=proyecto;Password=postgres;",
       "EnableAutoMigration": true
     }
   }
   ```

4. **Aplicar migraciones**
   ```bash
   dotnet ef database update
   ```

### Conexión Remota (Servidor)
```json
{
  "ConnectionString": "Host=servidor.com;Port=5432;Database=proyecto_db;Username=usuario;Password=contraseña;SSL Mode=Require;"
}
```

### Verificar conexión
```bash
psql -h localhost -U proyecto -d proyecto_db
```

---

## MySQL

### Instalación

1. **Instalar MySQL**
   - [Descargar MySQL](https://www.mysql.com/downloads/)
   - **Windows**: Usar el instalador MySQL Community Edition
   - **Linux**: `sudo apt-get install mysql-server`
   - **macOS**: `brew install mysql`

2. **Crear usuario y base de datos**
   ```bash
   # Conectarse a MySQL
   mysql -u root -p
   
   # Crear usuario (contraseña: root)
   CREATE USER 'proyecto'@'localhost' IDENTIFIED BY 'root';
   
   # Crear base de datos
   CREATE DATABASE proyecto_db;
   
   # Dar permisos
   GRANT ALL PRIVILEGES ON proyecto_db.* TO 'proyecto'@'localhost';
   FLUSH PRIVILEGES;
   
   # Salir
   exit
   ```

3. **Configurar la conexión en appsettings.json**
   ```json
   {
     "Database": {
       "Provider": "MySql",
       "ConnectionString": "Server=localhost;Port=3306;Database=proyecto_db;User=proyecto;Password=root;",
       "EnableAutoMigration": true
     }
   }
   ```

4. **Aplicar migraciones**
   ```bash
   dotnet ef database update
   ```

### Conexión Remota (Servidor)
```json
{
  "ConnectionString": "Server=servidor.com;Port=3306;Database=proyecto_db;User=usuario;Password=contraseña;"
}
```

### Verificar conexión
```bash
mysql -h localhost -u proyecto -p proyecto_db
```

---

## Docker (Opción alternativa rápida)

Si prefieres usar Docker para las bases de datos:

### PostgreSQL en Docker
```bash
docker run -d \
  --name postgres \
  -e POSTGRES_USER=proyecto \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=proyecto_db \
  -p 5432:5432 \
  postgres:15
```

### MySQL en Docker
```bash
docker run -d \
  --name mysql \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=proyecto_db \
  -e MYSQL_USER=proyecto \
  -e MYSQL_PASSWORD=root \
  -p 3306:3306 \
  mysql:8.0
```

---

## Pasos de desarrollo

### Primer inicio con nueva BD

1. **Cambiar el provider en appsettings.json**
   ```json
   "Provider": "PostgreSql"
   ```

2. **Crear la base de datos** (ver instrucciones arriba)

3. **Habilitar migraciones automáticas**
   ```json
   "EnableAutoMigration": true
   ```

4. **Ejecutar la aplicación**
   ```bash
   dotnet run
   ```

### Cuando la BD ya existe

1. **Cambiar el provider en appsettings.json**

2. **Actualizar la BD manualmente**
   ```bash
   dotnet ef database update
   ```

### Crear nuevas migraciones

Todas las migraciones funcionan igual con cualquier proveedor:

```bash
# Crear migración
dotnet ef migrations add NombreMigracion

# Ver migraciones
dotnet ef migrations list

# Actualizar BD
dotnet ef database update

# Revertir última migración
dotnet ef migrations remove
```

---

## Tabla de referencia rápida

| BD | Puerto | Usuario | Contraseña | BD por defecto |
|----|--------|---------|------------|----------------|
| SQL Server | 1433 | (Windows Auth) | - | master |
| PostgreSQL | 5432 | postgres | postgres | postgres |
| MySQL | 3306 | root | root | mysql |

---

## Verificación post-instalación

Ejecutar este comando después de aplicar migraciones:

```bash
# Ver todas las tablas creadas
dotnet ef dbcontext info
```

Deberías ver algo como:
```
Provider name: Microsoft.EntityFrameworkCore.SqlServer (o Npgsql / Pomelo)
Database name: ProyectoDb
Database version: ...
```

---

## Troubleshooting

### "Database connection failed"
- Verificar que el servidor está corriendo
- Comprobar la cadena de conexión
- Validar permisos de usuario

### "Migration failed"
- Ejecutar: `dotnet ef migrations remove`
- Crear nuevamente: `dotnet ef migrations add`
- Aplicar: `dotnet ef database update`

### "Provider version mismatch"
- Asegurarse de que EF Core y el provider compatible coincidan
- Ver versiones en `Project.Server.csproj`
