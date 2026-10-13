import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Approval inbox for every user and the flow editor for administrators. */
const manifest: ModuleManifest = {
  id: 'workflows',
  backendModule: 'workflows',
  i18n: resources,
  routes: [
    {
      path: 'workflows/inbox',
      lazy: async () => ({ Component: (await import('./pages/InboxPage')).InboxPage }),
      handle: { titleKey: 'nav.approvals' },
    },
    {
      path: 'workflows/definitions',
      lazy: async () => ({ Component: (await import('./pages/DefinitionsPage')).DefinitionsPage }),
      handle: { permission: 'workflows.definitions.manage', titleKey: 'nav.workflows' },
    },
  ],
};

export default manifest;
