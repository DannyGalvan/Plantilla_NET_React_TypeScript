import type { ModuleManifest } from '@/shared/modules/manifest';

import { tenancyResources } from './i18n';

const manifest: ModuleManifest = {
  id: 'tenancy',
  backendModule: 'tenancy',
  i18n: tenancyResources,
  routes: [
    {
      path: 'tenancy/tenants',
      lazy: async () => ({ Component: (await import('./pages/TenantsPage')).TenantsPage }),
      handle: { permission: 'tenancy.tenants.read', titleKey: 'nav.tenants' },
    },
    {
      path: 'tenancy/settings',
      lazy: async () => ({ Component: (await import('./pages/SettingsPage')).SettingsPage }),
      handle: { permission: 'tenancy.settings.read', titleKey: 'nav.settings' },
    },
  ],
};

export default manifest;
