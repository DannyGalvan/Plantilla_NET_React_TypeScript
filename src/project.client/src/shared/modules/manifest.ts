import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router';

import type { ModuleResources } from '@/shared/i18n';

/**
 * Contract of a frontend module. Creating `src/modules/<name>/module.ts` that exports a manifest registers the
 * module: routes, public routes and translations are discovered automatically (no central file to edit).
 */
export interface ModuleManifest {
  /** Module id, also the i18n namespace (for example `catalog`). */
  id: string;
  /** Backend module name; the module is skipped when the backend does not load it (Modules:Disabled). */
  backendModule?: string;
  /** Pages inside the authenticated shell. Use `handle.permission` to protect a route. */
  routes?: RouteObject[];
  /** Pages outside the shell (sign in, password reset). */
  publicRoutes?: RouteObject[];
  /** Translations of the module namespace. */
  i18n?: ModuleResources;
  /** Feature flag that hides the whole module when disabled for the tenant. */
  feature?: string;
  /** Cards the module contributes to the home dashboard (the dashboard knows no module). */
  widgets?: DashboardWidget[];
}

/** A card on the home dashboard, rendered only when the user holds `permission`. */
export interface DashboardWidget {
  id: string;
  component: ComponentType;
  permission?: string;
  /** Lower first. */
  order?: number;
  /** `kpi` cards share a row; `wide` cards take the full width. */
  size?: 'kpi' | 'wide';
}

/** Route metadata read by the router guards and the breadcrumbs. */
export interface RouteHandle {
  permission?: string;
  titleKey?: string;
}
