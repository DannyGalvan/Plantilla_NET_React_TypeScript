import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Recurring jobs declared by the modules: schedule, pause, manual runs and history per tenant. */
const manifest: ModuleManifest = {
  id: 'jobs',
  backendModule: 'jobs',
  i18n: resources,
  routes: [
    {
      path: 'jobs',
      lazy: async () => ({ Component: (await import('./pages/JobsPage')).JobsPage }),
      handle: { permission: 'jobs.jobs.manage', titleKey: 'nav.jobs' },
    },
  ],
};

export default manifest;
