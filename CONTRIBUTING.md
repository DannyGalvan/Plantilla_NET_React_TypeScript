# Contribuir

## Flujo de trabajo

1. Crea una rama desde `master`: `feat/<tema>`, `fix/<tema>` o `docs/<tema>`.
2. Haz commits pequeños con [Conventional Commits](https://www.conventionalcommits.org/): `feat(catalog): ...`, `fix(identity): ...`.
3. Antes de abrir el PR, ejecuta lo mismo que CI:

   ```bash
   dotnet build Project.slnx          # las advertencias son errores
   dotnet test Project.slnx           # requiere Docker (Testcontainers)
   cd src/project.client && pnpm verify && pnpm build
   node scripts/check-docs.mjs
   ```

4. Abre el PR contra `master` describiendo el cambio, cómo se probó y si afecta migraciones o configuración.

## Reglas del código

- Respeta las reglas de dependencia de [docs/05-arquitectura.md](docs/05-arquitectura.md); las pruebas de arquitectura fallan si no.
- Todo método async recibe y propaga `CancellationToken`. Usa `TimeProvider`, nunca `DateTime.Now`.
- Permisos con formato `{modulo}.{recurso}.{accion}` declarados como constantes en `Contracts`.
- Cada endpoint declara `RequirePermission`, `RequireAuthenticatedUser` o `AllowAnonymous`; si no, la API no arranca.
- Cambios de modelo: genera la migración en los tres motores con `scripts/add-migration`.
- Frontend: solo tokens semánticos de color (`bg-primary`, `text-muted-foreground`), nunca paletas crudas ni hex.
- Sin secretos en el repositorio. Usa user-secrets o variables de entorno.
- Documentación en español, sin emojis.

## Decisiones de arquitectura

Las decisiones relevantes se registran como ADR en [docs/adr](docs/adr). Para proponer un cambio de fondo (por ejemplo
una dependencia nueva o un patrón distinto), abre un PR con un ADR nuevo. Las dependencias deben tener licencia MIT,
Apache 2.0 o BSD; las licencias copyleft fuertes o comerciales requieren un ADR aprobado.
