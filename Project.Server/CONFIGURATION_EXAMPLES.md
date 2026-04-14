# Ejemplos de configuración por proveedor

Este archivo muestra ejemplos de configuración para cada proveedor soportado.

## SQL Server

### LocalDB (Desarrollo - Recomendado)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### SQL Server Express (Desarrollo)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=NOMBRE_PC\\SQLEXPRESS;Database=ProyectoDb;Trusted_Connection=true;TrustServerCertificate=true;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### SQL Server Remoto (Producción)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=sql.ejemplo.com;Database=ProyectoDb;User Id=usuario;Password=ContraseñaSegura123!;Encrypt=true;TrustServerCertificate=false;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Azure SQL Database
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=tcp:servidor.database.windows.net,1433;Initial Catalog=ProyectoDb;Persist Security Info=False;User ID=usuario@servidor;Password=ContraseñaSegura123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

---

## PostgreSQL

### Local (Desarrollo - Recomendado)
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Port=5432;Database=proyecto_db;Username=proyecto;Password=proyecto;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Docker Local
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=postgres;Port=5432;Database=proyecto_db;Username=proyecto;Password=proyecto;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### PostgreSQL Remoto (Producción)
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=postgres.ejemplo.com;Port=5432;Database=proyecto_db;Username=usuario;Password=ContraseñaSegura123!;SSL Mode=Require;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### AWS RDS PostgreSQL
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=proyecto.12345678.us-east-1.rds.amazonaws.com;Port=5432;Database=proyecto_db;Username=admin;Password=ContraseñaSegura123!;SSL Mode=Require;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Heroku PostgreSQL
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=ec2-12-34-567-890.compute-1.amazonaws.com;Port=5432;Database=abc123;Username=user;Password=pass;SSL Mode=Require;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

---

## MySQL

### Local (Desarrollo - Recomendado)
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Port=3306;Database=proyecto_db;User=proyecto;Password=proyecto;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Docker Local
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=mysql;Port=3306;Database=proyecto_db;User=proyecto;Password=proyecto;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### MySQL Remoto (Producción)
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=mysql.ejemplo.com;Port=3306;Database=proyecto_db;User=usuario;Password=ContraseñaSegura123!;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### AWS RDS MySQL
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=proyecto.12345678.us-east-1.rds.amazonaws.com;Port=3306;Database=proyecto_db;User=admin;Password=ContraseñaSegura123!;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Google Cloud SQL MySQL
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=35.192.123.45;Port=3306;Database=proyecto_db;User=root;Password=ContraseñaSegura123!;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

---

## Configuración por ambiente

### Development (appsettings.Development.json)
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Port=5432;Database=proyecto_dev;Username=dev;Password=dev;",
    "EnableAutoMigration": true,
    "EnableSensitiveDataLogging": true,
    "EnableChangeTracking": true
  }
}
```

### Staging (appsettings.Staging.json)
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=postgres-staging.ejemplo.com;Port=5432;Database=proyecto_staging;Username=staging_user;Password=StrongPassword123!;SSL Mode=Require;",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "${DB_CONNECTION_STRING}",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

---

## Usando variables de entorno (Recomendado en Producción)

### Windows
```batch
setx DB_PROVIDER PostgreSql
setx DB_CONNECTION_STRING "Host=prod.ejemplo.com;Port=5432;Database=proyecto;Username=user;Password=pass;"
```

### Linux/macOS
```bash
export DB_PROVIDER=PostgreSql
export DB_CONNECTION_STRING="Host=prod.ejemplo.com;Port=5432;Database=proyecto;Username=user;Password=pass;"
```

### En appsettings.json
```json
{
  "Database": {
    "Provider": "${DB_PROVIDER:SqlServer}",
    "ConnectionString": "${DB_CONNECTION_STRING}",
    "EnableAutoMigration": false,
    "EnableSensitiveDataLogging": false,
    "EnableChangeTracking": true
  }
}
```

---

## Docker Compose (Fácil para desarrollo)

### docker-compose.yml
```yaml
version: '3.8'

services:
  # PostgreSQL
  postgres:
    image: postgres:15
    environment:
      POSTGRES_USER: proyecto
      POSTGRES_PASSWORD: proyecto
      POSTGRES_DB: proyecto_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  # MySQL
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_USER: proyecto
      MYSQL_PASSWORD: proyecto
      MYSQL_DATABASE: proyecto_db
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  postgres_data:
  mysql_data:
```

### Iniciar con Docker Compose
```bash
# Iniciar servicios
docker-compose up -d

# Detener servicios
docker-compose down
```

---

## Cheat sheet - Conexiones rápidas

| BD | Host | Puerto | Usuario | Contraseña | Base datos |
|----|------|--------|---------|-----------|-----------|
| SQL Server LocalDB | (localdb)\mssqllocaldb | - | Windows Auth | - | ProyectoDb |
| PostgreSQL | localhost | 5432 | postgres | postgres | proyecto_db |
| MySQL | localhost | 3306 | root | root | proyecto_db |
| Docker PostgreSQL | postgres | 5432 | proyecto | proyecto | proyecto_db |
| Docker MySQL | mysql | 3306 | root | root | proyecto_db |

---

## ¡Selecciona la configuración que necesitas y cópiala!
