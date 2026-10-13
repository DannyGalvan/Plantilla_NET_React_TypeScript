import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Change history of every module. Records show their own history with the shared HistoryPanel. */
const manifest: ModuleManifest = {
  id: 'audit',
  backendModule: 'audit',
  i18n: resources,
  routes: [
    {
      path: 'audit',
      lazy: async () => ({ Component: (await import('./pages/AuditPage')).AuditPage }),
      handle: { permission: 'audit.history.view', titleKey: 'nav.audit' },
    },
  ],
};

export default manifest;
