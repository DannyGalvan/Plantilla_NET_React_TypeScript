import type { ModuleManifest } from '@/shared/modules/manifest';

import { organizationResources } from './i18n';

/** Optional: disappears automatically when the backend does not load the Organization module. */
const manifest: ModuleManifest = {
  id: 'organization',
  backendModule: 'organization',
  i18n: organizationResources,
  routes: [
    {
      path: 'organization/units',
      lazy: async () => ({ Component: (await import('./pages/UnitsPage')).UnitsPage }),
      handle: { permission: 'organization.units.read', titleKey: 'nav.organization' },
    },
    {
      path: 'organization/unit-types',
      lazy: async () => ({ Component: (await import('./pages/UnitTypesPage')).UnitTypesPage }),
      handle: { permission: 'organization.unit-types.read', titleKey: 'nav.unitTypes' },
    },
  ],
};

export default manifest;
