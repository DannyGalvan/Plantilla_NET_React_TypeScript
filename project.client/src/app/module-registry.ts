import type { RouteObject } from 'react-router';

export interface NavItem {
  key: string;
  labelKey: string;
  icon: string;
  to: string;
  permission?: string;
  order: number;
  children?: NavItem[];
}

export interface ModuleManifest {
  id: string;
  routes: RouteObject[];
  nav?: NavItem[];
  features?: string[];
  i18n?: Record<'es' | 'en', () => Promise<Record<string, unknown>>>;
}

/**
 * Manifest-driven module system. Each module exports a \`module.ts\` that the
 * eager glob below picks up. \`ModuleDisabled\` config picks them up. Adding
 * a new module = drop a folder under \`src/modules/<name>\`; nothing else.
 */
interface ManifestModule {
  default: ModuleManifest;
}

const manifestMap: Record<string, ManifestModule> =
  import.meta.glob<ManifestModule>('../modules/*/module.ts', { eager: true });

/**
 * Synchronous access to the manifests collected at startup. Used by
 * \`App.tsx\` to build the router and the menu without an extra round-trip.
 */
export function getAllManifests(): ModuleManifest[] {
  return Object.values(manifestMap).map((entry) => entry.default);
}
