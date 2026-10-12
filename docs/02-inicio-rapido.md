# Inicio rápido

**Para qué sirve este documento:** levantar la plantilla en tu máquina de una de tres formas.
**Requisitos previos:** [01-prerequisitos.md](01-prerequisitos.md). **Tiempo estimado:** 10 minutos.

## A. Solo Docker (sin SDKs)

```bash
cp deploy/docker/.env.example deploy/docker/.env      # edita los valores <...>
docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/docker/.env up -d --build
```

```powershell
Copy-Item deploy/docker/.env.example deploy/docker/.env
docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/docker/.env up -d --build
```

Abre `http://localhost:8080`. Usuario `admin`, contraseña `ADMIN_PASSWORD` del archivo `.env`.

## B. Nativo con la base de datos en Docker (recomendado para desarrollo)

```bash
docker compose --profile postgres up -d          # o sqlserver | mysql
./scripts/setup.sh                               # elige motor y modo; guarda secretos en user-secrets
dotnet run --project src/Host/Project.Api
```

```powershell
docker compose --profile postgres up -d
./scripts/setup.ps1
dotnet run --project src/Host/Project.Api
```

En Development la API aplica migraciones y semillas al iniciar (`Database:MigrateOnStartup=true`) y arranca Vite
automáticamente (SPA Proxy). Abre `https://localhost:7180`; el navegador se redirige a la SPA en `https://localhost:5173`.

Para ejecutar frontend y backend por separado:

```bash
dotnet run --project src/Host/Project.Api --launch-profile api-only    # API + Scalar en /scalar
cd src/project.client && pnpm dev                                      # SPA en https://localhost:5173
```

## C. Nativo con una base de datos instalada en tu equipo

Igual que B, pero en `setup` indica la cadena de conexión de tu servidor. El usuario necesita permiso para crear
la base de datos o la base debe existir vacía.

## Cómo verificar que funcionó

- `GET https://localhost:7180/health/ready` responde `{"status":"Healthy",...}`.
- Puedes iniciar sesión con `admin` y la contraseña que imprimió `setup`; el sistema pide cambiarla.
- En la página de inicio ves los indicadores de productos y categorías del módulo de ejemplo.

## Problemas comunes

Certificados de desarrollo, puertos ocupados o conexión a la base: ver [troubleshooting.md](troubleshooting.md).
