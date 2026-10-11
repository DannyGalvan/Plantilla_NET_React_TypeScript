# Glosario

> Definiciones cortas de los términos específicos del proyecto.

| Término | Definición |
|---|---|
| **Aggregate Root** | Entidad que es la raíz de un agregado: controla las invariantes del agregado y emite eventos de dominio. Ver `Project.SharedKernel.Domain.AggregateRoot<TId>`. |
| **BuildingBlock** | Proyecto compartido por todos los módulos. Hay 4: `SharedKernel`, `Application`, `Infrastructure`, `Web`. |
| **Comando (CQRS)** | Acción que muta estado. Implementa `ICommand` o `ICommand<T>`. |
| **Contrato** | Interfaz entre módulos. Vive en `Contracts`. Un módulo expone sus eventos de dominio + constantes de permisos; nunca expone su `Domain` o `Application`. |
| **DataScope** | `Platform` o `Tenant`. Determina en qué BD vive el dato y qué conexión usar. Lo declara cada `IModule`. |
| **Domain** | Capa interna del módulo: entidades, value objects, eventos de dominio. Solo depende de `SharedKernel`. |
| **Endpoint fail-closed** | Cada endpoint expone `[RequireOperation]` o `[AllowAnonymous]`. Sin uno de los dos, la convención de arranque falla el host. |
| **Evento de dominio** | Notificación inmutable que un aggregate emite al cambiar estado. Vive en `Domain/Events/`. |
| **Evento de integración** | Mensaje que cruza la frontera de un módulo. Vive en `Contracts/`. |
| **Feature Flag** | Switch por tenant + plan + unidad organizativa. Cableado en `/platform/config.features`. |
| **Handler** | Caso de uso único. `sealed`, una clase por acción. Devuelve `Result<T>` o `Result`. |
| **IModule** | Interfaz que todo módulo implementa para enchufarse al host: `AddServices` + `MapEndpoints`. |
| **IMultiTenant** | Marca que activa el filtro global `TenantId == current` en `ModuleDbContext`. |
| **ISoftDeletable** | Marca que activa el borrado lógico vía `SoftDeleteInterceptor`. **Obligatoria** en entidades de negocio. |
| **ITenantContext** | Servicio scoped que lleva el `TenantInfo` actual. `Mode=None` devuelve un tenant `default` sin I/O. |
| **Mediator** | Bus interno de mensajes. `Mediator.Abstractions` + `Mediator.SourceGenerator` (MIT). |
| **Migración por proveedor** | Migración EF Core generada por cada `Project.Migrations.{Provider}`. Permite cambios específicos del motor. |
| **Módulo** | Unidad de empaquetado. 4 csproj (Domain/Application/Infrastructure/Contracts) + `IModule` + endpoints. |
| **OrganizationContext** | Servicio scoped con `ActiveUnitId` y `AccessibleUnitIds`. Lo provee `OrganizationModule`. |
| **OrganizationUnit** | Unidad organizativa (Company/Brand/Country/Branch). Jerarquía vía columna `Path` materializada. |
| **Outbox** | Tabla `OutboxMessages` por módulo. Eventos de dominio se serializan ahí en la misma transacción y se publican asíncronamente. |
| **Permissions Version** (`pv`) | Entero que el `User` lleva. Cuando `RolePermissionsChanged` se incrementa, invalida `perm:{tid}:{userId}:{pv}`. |
| **Permissions cache key** | `perm:{tid}:{userId}:{pv}`. El más específico es `perm:{tenant}:{userId}:{pv}:{ou}`. |
| **Plan maestro** | `plans/2026-10-10_refactor-plantilla-modular-empresarial.md`. Define fases, criterios de salida y desviaciones. |
| **Query (CQRS)** | Acción de lectura. Implementa `IQuery<T>`. |
| **Result<T>** | Éxito o error tipado. `Error` lleva `Code`, `Description`, `Type`, `Details`. Mapea a `ProblemDetails` HTTP. |
| **Settings en cascada** | JSON-merge: defaults → appsettings → System → Tenant → OrgUnit (root → active) → User. |
| **SharedKernel** | Tipos compartidos sin dependencias (Entity, AggregateRoot, Result, Error). |
| **Spec** (`ISpecification<T>`) | Composable criteria + includes + order + paging para queries. |
| **ThemeCustomizer** | Sheet accesible desde el Topbar para cambiar mode/preset/density/fontScale/primaryOverride/radius. |
| **ThemePreset** | `default` / `ocean` / `forest` / `sunset` / `corporate`. Aplicado vía `[data-theme=...]`. |
| **Tokens CSS en OKLCH** | `src/shared/theme/tokens.css`. Única fuente de color. Consumido por Tailwind v4 vía `@theme inline`. |
| **Tenancy:Mode** | `None` / `SharedDatabase` / `DatabasePerTenant` / `Hybrid`. Cambia el comportamiento de los filtros y de la conexión. |
| **Unit of Work** | `IUnitOfWork` (abstracción) + `EfUnitOfWork<TContext>` (impl). Agrupa SaveChanges dentro de una transacción. |
| **Verification** | `dotnet build Project.sln --configuration Release -warnaserror && dotnet test ... && pnpm verify`. |

## Cómo agregar un término

Edita este archivo y añade la fila en orden alfabético. Si la definición
excede dos líneas, enlaza a la guía correspondiente en `docs/guias/` o
`docs/adr/`.