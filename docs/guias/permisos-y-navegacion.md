# Permisos y navegación

> Cómo declarar permisos, gatear endpoints y construir el menú lateral.

## Formato

`{module}.{resource}.{action}` en minúsculas. Constantes en
`Contracts/Permissions.cs` del módulo. Nunca derivados del nombre del
controller.

```csharp
public static class InventoryPermissions
{
    public const string ProductsRead   = "inventory.products.read";
    public const string ProductsCreate = "inventory.products.create";
    public const string ProductsUpdate = "inventory.products.update";
    public const string ProductsDelete = "inventory.products.delete";
}
```

## Backend

### Aplicar el permiso

```csharp
app.MapCrud<Product, Guid, ProductDto, CreateProductDto, UpdateProductDto>(
    "products", "inventory.products");
```

`MapCrud` añade `RequirePermission("inventory.products.read")` a `GET /`,
`RequirePermission("inventory.products.create")` a `POST /`,
etc. La metadata vive en `PermissionAttribute`.

### Permitir sin auth

```csharp
app.MapGet("/platform/config", ...).AllowAnonymous();
```

### Fail-closed (Fase 5)

`RequireOperationConvention` recorre todos los endpoints registrados al
arrancar. Si alguno no tiene `[RequireOperation]` ni `[AllowAnonymous]`,
el host no arranca. Esto fuerza la convención en CI.

## Frontend

### Proteger una ruta

```tsx
<Route element={<RequirePermission permission="inventory.products.create" />}>
  <Route path="/products/new" element={<ProductFormPage />} />
</Route>
```

`RequirePermission` lee `usePermission(perm)`. Si el usuario no la tiene,
redirige a `/` con un toast. Si no hay sesión, `RequireAuth` (más
externo) redirige a `/login`.

### Gatear un botón

```tsx
const canCreate = usePermission('inventory.products.create');
{canCreate && <Button onClick={openCreateDialog}>Nuevo producto</Button>}
```

### Menú lateral

Cada módulo declara su `ModuleManifest.nav`:

```ts
export default {
  id: 'inventory',
  routes: [...],
  nav: [
    {
      key: 'inventory.products',
      labelKey: 'inventory.nav.products',
      icon: 'package',
      to: '/inventory/products',
      permission: 'inventory.products.read',
      order: 50,
    },
  ],
};
```

El `AppShell` (en `src/components/app-shell/Sidebar.tsx`, cuando llegue
Fase 8 slice 2) renderiza el menú:

- Itera los manifests en orden de `id`.
- Filtra los items por permiso del usuario.
- Agrupa por módulo.

El menú se renderiza solo si el usuario tiene permiso; nunca items sin
permiso.

## Cómo se invalidan los permisos

`RolePermissionsChanged` (evento de dominio cuando un admin cambia los
permisos de un rol) incrementa `pv` del usuario afectado. El
`HybridCachePermissionService` invalida la cache `perm:{tid}:{userId}:{pv}`.
Los siguientes requests resuelven permisos actualizados **sin re-login**.

## Buenas prácticas

- **No derives permisos del nombre del controller**. Si renombras
  `UserController` a `AccountController`, rompes a todos los usuarios que
  ya tenían el token viejo.
- **No uses `*` como permiso**. La cache key incluye la acción; si
  permites `inventory.*`, no hay forma de invalidar una acción concreta.
- **No metas permisos en el JWT**. El token lleva `sub`/`tid`/`pv`. Los
  permisos vienen de `IPermissionService` con cache.
- **No leas `User.Roles` directamente**. Usa `IPermissionService.HasAsync`
  para chequear; respeta el cache y la invalidación.