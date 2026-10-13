import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Outgoing webhooks of the tenant: subscriptions, signing secret, delivery log and retries. */
const manifest: ModuleManifest = {
  id: 'webhooks',
  backendModule: 'webhooks',
  i18n: resources,
  routes: [
    {
      path: 'webhooks',
      lazy: async () => ({ Component: (await import('./pages/WebhooksPage')).WebhooksPage }),
      handle: { permission: 'webhooks.subscriptions.manage', titleKey: 'nav.webhooks' },
    },
  ],
};

export default manifest;
