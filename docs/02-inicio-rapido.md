# 02 · Inicio rápido

> Cuatro caminos para arrancar la plantilla. Elige el que se ajuste a tu
> entorno.

## Camino A — Solo Docker (sin SDK local)

Si todavía no tienes .NET ni Node instalados, o quieres revisar la
plantilla sin tocar tu máquina:

```bash
git clone https://github.com/DannyGalvan/Plantilla_NET_React_TypeScript
cd Plantilla_NET_React_TypeScript
docker compose --profile postgres up -d   # o --profile sqlserver / mysql
docker compose up migrate                 # cuando el backend esté empaquetado
docker compose up api
```

Después abre `https://localhost:8443` con el usuario `1234` y la contraseña
`1234567` (configurable en `Seed__AdminPassword`). Cambia la contraseña en
el primer login.

## Camino B — Aspire (recomendado para desarrollo)

Aspire orquesta Postgres + Redis + MigrationService + Api + Vite en un solo
comando. El dashboard abre en la URL que imprime.

```bash
git clone https://github.com/DannyGalvan/Plantilla_NET_React_TypeScript
cd Plantilla_NET_React_TypeScript
dotnet run --project src/Host/Project.AppHost
```

El dashboard muestra logs estructurados de cada servicio y un `endpoint`
para abrir el frontend.

## Camino C — Nativo con BD en Docker

```bash
docker compose --profile postgres up -d db redis   # o --profile sqlserver / mysql
cd src/Host/Project.MigrationService
Database__Provider=PostgreSql \
  ConnectionStrings__Platform=Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla \
  dotnet run -- migrate
cd ../Project.Api
Database__Provider=PostgreSql \
  ConnectionStrings__Platform=Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla \
  ConnectionStrings__Identity=Host=localhost;Database=plantilla_identity;Username=plantilla;Password=plantilla \
  Jwt__SigningKey=REPLACE-WITH-AT-LEAST-32-RANDOM-BYTES \
  Seed__AdminPassword=Choose-A-Strong-Password \
  dotnet run
```

En otra terminal:

```bash
cd project.client
pnpm install
pnpm dev
```

El frontend corre en `https://localhost:5173`. Vite proxea las llamadas a
`https://localhost:5050`.

## Camino D — Nativo con BD instalada localmente

Igual que el camino C, pero la conexión apunta a tu instancia local
(`localhost:1433`, `localhost:5432` o `localhost:3306` según motor).

## Cómo verificar que funcionó

```bash
curl -i http://localhost:5050/health/live
# 200 OK
curl -i http://localhost:5050/platform/config
# 200 OK con appName, branding, features
curl -i http://localhost:5173
# 200 OK con la SPA
```

Si algún endpoint no responde, [`docs/troubleshooting.md`](troubleshooting.md)
tiene los errores más comunes por SO y por motor.

## Siguiente paso

- Lee [`docs/03-configuracion-inicial.md`](03-configuracion-inicial.md) para
  el checklist de día 1 (perfil, motor de BD, secretos, marca, idioma).
- O ve directo a [`docs/07-agregar-un-modulo.md`](07-agregar-un-modulo.md) si
  ya tienes la app corriendo y vas a extender.