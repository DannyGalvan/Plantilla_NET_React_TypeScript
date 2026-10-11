# Despliegue con Docker

> Imagen multi-stage basada en el SDK .NET 10 oficial. La SPA se compila
> dentro de la imagen, así el artefacto es único y la cookie de refresh no
> cruza dominios.

## `deploy/docker/Dockerfile.api`

```dockerfile
# ---- build ----
FROM node:24-alpine AS client
WORKDIR /client
COPY project.client/package*.json project.client/pnpm-lock.yaml ./
RUN corepack enable && pnpm install --frozen-lockfile
COPY project.client ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src ./src
COPY Project.Server Project.Server.Tests Project.sln ./
RUN dotnet restore Project.sln && \
    dotnet publish src/Host/Project.Api/Project.Api.csproj \
    -c Release -o /app/publish \
    --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=client /client/dist ./wwwroot
COPY --from=build /app/publish ./
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q -O- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Project.Api.dll"]
```

## `deploy/docker/Dockerfile.migrations`

Mismo patrón, multi-stage hasta `dotnet publish` con
`src/Host/Project.MigrationService/Project.MigrationService.csproj`. No
incluye la SPA. Imagen final con `mcr.microsoft.com/dotnet/runtime:10.0`.

## `deploy/docker/compose.yml`

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: plantilla
      POSTGRES_PASSWORD: plantilla
      POSTGRES_DB: plantilla
    volumes: ["pgdata:/var/lib/postgresql/data"]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U plantilla"]
      interval: 5s
      retries: 30

  redis:
    image: redis:7-alpine
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]

  migrations:
    build:
      context: ../..
      dockerfile: deploy/docker/Dockerfile.migrations
    environment:
      Database__Provider: PostgreSql
      ConnectionStrings__Platform: "Host=db;Database=plantilla;Username=plantilla;Password=plantilla"
    depends_on:
      db: { condition: service_healthy }

  api:
    build:
      context: ../..
      dockerfile: deploy/docker/Dockerfile.api
    environment:
      Database__Provider: PostgreSql
      ConnectionStrings__Platform: "Host=db;Database=plantilla;Username=plantilla;Password=plantilla"
      ConnectionStrings__Identity: "Host=db;Database=plantilla_identity;Username=plantilla;Password=plantilla"
      Cache__Redis__ConnectionString: "redis:6379"
      Jwt__SigningKey: "${JWT_SIGNING_KEY:?set JWT_SIGNING_KEY}"
      Seed__AdminPassword: "${SEED_ADMIN_PASSWORD:?set SEED_ADMIN_PASSWORD}"
    ports: ["8080:8080"]
    depends_on:
      db: { condition: service_healthy }
      migrations: { condition: service_completed_successfully }
      redis: { condition: service_healthy }

volumes:
  pgdata:
```

## Comandos

```bash
# Build
docker compose -f deploy/docker/compose.yml build

# Setear secretos
export JWT_SIGNING_KEY=$(openssl rand -base64 48)
export SEED_ADMIN_PASSWORD=$(openssl rand -base64 16)

# Levantar (migrations se aplican antes que api por depends_on)
docker compose -f deploy/docker/compose.yml up -d

# Escalar el API a 3 réplicas (Redis obligatorio para rate limit y permisos)
docker compose -f deploy/docker/compose.yml up -d --scale api=3
```

## Publicar la imagen

```bash
docker tag plantilla/api:latest ghcr.io/<org>/plantilla-api:1.0.0
docker push ghcr.io/<org>/plantilla-api:1.0.0
```

Alternativa sin Dockerfile (publica el contenedor desde el SDK):

```bash
dotnet publish src/Host/Project.Api/Project.Api.csproj \
  /t:PublishContainer \
  -c Release \
  /p:ContainerImageTag=1.0.0 \
  /p:ContainerRegistry=ghcr.io/<org>
```

## Persistencia y backup

- Volumen `pgdata` mantiene los datos entre reinicios.
- Backup diario:
  ```bash
  docker exec plantilla-db pg_dump -U plantilla plantilla | gzip > backup-$(date +%F).sql.gz
  ```
- Restaurar:
  ```bash
  gunzip < backup.sql.gz | docker exec -i plantilla-db psql -U plantilla plantilla
  ```