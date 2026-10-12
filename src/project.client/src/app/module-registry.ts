import type { ModuleResources } from '@/shared/i18n';
import type { ModuleManifest } from '@/shared/modules/manifest';
import type { PlatformConfig } from '@/shared/platform/platform-config';

/** Every `src/modules/<name>/module.ts` is a module. Adding a folder registers it; nothing else to edit. */
const discovered = Object.values(
  import.meta.glob<{ default: ModuleManifest }>('../modules/*/module.{ts,tsx}', { eager: true }),
).map((entry) => entry.default);

export function allModules(): ModuleManifest[] {
  return discovered;
}

/** Modules active for this deployment: the backend module is loaded and the feature (if any) is enabled. */
export function activeModules(config: PlatformConfig): ModuleManifest[] {
  return discovered.filter(
    (module) =>
      (!module.backendModule ||
        config.modules.length === 0 ||
        config.modules.includes(module.backendModule.toLowerCase())) &&
      (!module.feature || config.features.includes(module.feature)),
  );
}

export function moduleResources(): Record<string, ModuleResources> {
  return Object.fromEntries(discovered.filter((module) => module.i18n).map((module) => [module.id, module.i18n!]));
}
