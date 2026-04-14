# 📋 QUICK REFERENCE - Cambiar Base de Datos

## En 3 pasos

### 1️⃣ Editar appsettings.json
```json
{
  "Database": {
    "Provider": "PostgreSql",  // ← CAMBIAR AQUÍ
    "ConnectionString": "Host=localhost;Port=5432;Database=proyecto_db;Username=usuario;Password=contraseña;",
    "EnableAutoMigration": true
  }
}
```

### 2️⃣ Ejecutar migración
```bash
dotnet ef database update
```

⚠️ **NOTA**: Si usas Package Manager Console en Visual Studio, usa:
```powershell
EntityFrameworkCore\Update-Database
```

❌ **NO uses** `update-database` (sin prefijo), ese es para EF6, no EF Core

### 3️⃣ ¡Listo!
Tu aplicación ahora usa PostgreSQL.

---

## 📌 Cadenas de conexión listas para copiar

### SQL Server (LocalDB)
```
Data Source=(localdb)\mssqllocaldb;Initial Catalog=ProyectoDb;Integrated Security=true;TrustServerCertificate=true;
```

### PostgreSQL (Local)
```
Host=localhost;Port=5432;Database=proyecto_db;Username=postgres;Password=postgres;
```

### MySQL (Local)
```
Server=localhost;Port=3306;Database=proyecto_db;User=root;Password=root;
```

---

## 🎯 Valores válidos para Provider

| Nombre | Usar en appsettings |
|--------|-------------------|
| Microsoft SQL Server | `"SqlServer"` |
| PostgreSQL | `"PostgreSql"` |
| MySQL | `"MySql"` |

---

## 🐳 Iniciar BD con Docker

### PostgreSQL
```bash
docker run -d \
  --name postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=proyecto_db \
  -p 5432:5432 \
  postgres:15
```

### MySQL
```bash
docker run -d \
  --name mysql \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=proyecto_db \
  -p 3306:3306 \
  mysql:8.0
```

---

## ✅ Checklist mínimo

- [ ] BD server corriendo
- [ ] Cadena de conexión correcta
- [ ] Provider escrito correctamente
- [ ] Ejecutar `dotnet ef database update`
- [ ] Probar endpoint

---

## 🆘 Problema? Ver:

- **Problemas comunes**: TROUBLESHOOTING.md
- **Ejemplos de configuración**: CONFIGURATION_EXAMPLES.md
- **Documentación técnica**: Infrastructure/Database/README.md
- **Guía de instalación**: SETUP_DATABASE.md
