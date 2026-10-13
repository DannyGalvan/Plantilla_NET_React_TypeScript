# Permisos y navegación

## Declarar

```csharp
// Contracts
public static class InventoryPermissions
{
    public const string Module = "inventory";
    public static class Warehouses { public const string Prefix = "inventory.warehouses"; public const string Read = Prefix + ".read"; }
}

// Application: descubierto automáticamente por AddModuleConventions
public sealed class InventoryPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() => [ new(InventoryPermissions.Warehouses.Read, "inventory", "permissions.inventory.warehouses.read", "warehouses") ];
    public IEnumerable<NavigationDefinition> GetNavigation() =>
        [ new("inventory.warehouses", "inventory", "inventory:warehouses.title", "/inventory/warehouses", "layers", InventoryPermissions.Warehouses.Read, 500) ];
}
```

`MapCrud` usa `<prefijo>.read|create|update|delete|restore`. Los endpoints propios usan `.RequirePermission("...")`.

## Sincronización

El migrador inserta los permisos nuevos, elimina los que ya no se declaran (también de los roles), da todos al rol
`SuperAdmin` y todos los asignables al rol `Administrator` de cada tenant.

## Menú

`GET /api/v1/me/navigation` devuelve las entradas filtradas por permiso y feature flag. La SPA las dibuja con el icono
indicado (mapa en `src/shared/components/icon.tsx`) y la clave de traducción `labelKey`.

## Frontend

- Rutas: `handle: { permission: 'inventory.warehouses.read' }` muestra 403 si falta.
- Acciones: `<Can permission="inventory.warehouses.create">...</Can>` o `usePermission(...)`.
- Son solo mejoras de experiencia: la API valida siempre.
