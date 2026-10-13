import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Runtime configuration of the platform (platform administrators only). */
const manifest: ModuleManifest = {
  id: 'configuration',
  backendModule: 'configuration',
  i18n: resources,
  routes: [
    {
      path: 'configuration',
      lazy: async () => ({ Component: (await import('./pages/ConfigurationPage')).ConfigurationPage }),
      handle: { permission: 'configuration.platform.manage', titleKey: 'nav.platformConfiguration' },
    },
  ],
};

export default manifest;
