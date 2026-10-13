import type { ModuleManifest } from './manifest';

let active: ModuleManifest[] = [];

/** Set once at bootstrap with the modules active for this deployment and tenant. */
export function setActiveModules(modules: ModuleManifest[]) {
  active = modules;
}

/** Modules active for this deployment (backend module loaded and feature enabled). */
export function getActiveModules(): ModuleManifest[] {
  return active;
}
