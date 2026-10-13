# Despliegue con Docker

**Para qué sirve este documento:** construir las imágenes y ejecutar la aplicación con Docker Compose.
**Requisitos previos:** Docker con Compose v2. **Tiempo estimado:** 10 minutos.

## Artefactos (`deploy/docker`)

| Archivo | Contenido |
|---|---|
| `Dockerfile` | Etapa Node (compila la SPA), etapa SDK (publica la API), imagen final `aspnet:10.0` con usuario no root, puerto 8080 y `HEALTHCHECK` |
| `Dockerfile.migrations` | Imagen del worker de migraciones (sin SPA) |
| `docker-compose.prod.yml` | `db` (PostgreSQL), `redis`, `migrations` (se ejecuta y termina) y `api` |
| `.env.example` | Variables a completar |

## Ejecutar

```bash
cp deploy/docker/.env.example deploy/docker/.env
openssl rand -base64 32                       # pega el resultado en MASTER_KEY (una sola vez, guárdala en tu bóveda)
docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/docker/.env up -d --build
```

```powershell
Copy-Item deploy/docker/.env.example deploy/docker/.env
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))   # MASTER_KEY
docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/docker/.env up -d --build
```

El servicio `api` espera a que `migrations` termine con éxito. Abre `http://localhost:8080`.

## Cómo verificar que funcionó

```bash
curl -s http://localhost:8080/health/ready          # {"status":"Healthy",...}
docker compose -f deploy/docker/docker-compose.prod.yml logs api
```

## Construir y publicar imágenes

```bash
docker build -f deploy/docker/Dockerfile -t ghcr.io/<org>/project-api:1.0.0 .
docker build -f deploy/docker/Dockerfile.migrations -t ghcr.io/<org>/project-migrations:1.0.0 .
docker push ghcr.io/<org>/project-api:1.0.0 && docker push ghcr.io/<org>/project-migrations:1.0.0
```

## Producción

- Coloca un proxy con TLS delante (nginx, Caddy, Traefik o el balanceador de la nube) y configura
  `Hosting__KnownNetworks__0` con la red del proxy para que se respete `X-Forwarded-For`.
- Otro motor: cambia el servicio `db` y `Database__Provider` / `Database__ConnectionStrings__Default`.
- Escalar: `docker compose ... up -d --scale api=3` detrás del proxy. Con más de una instancia Redis es obligatorio
  (ya incluido) para caché y permisos compartidos.
- Respaldo: el volumen de la base de datos (`pg_dump` periódico).

## Problemas comunes

Ver [troubleshooting.md](../troubleshooting.md#docker).
