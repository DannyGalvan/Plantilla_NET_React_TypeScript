import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';
import { NotificationBell } from './NotificationBell';

/** In-app inbox (bell in the top bar) and the notification templates of the tenant. */
const manifest: ModuleManifest = {
  id: 'notifications',
  backendModule: 'notifications',
  i18n: resources,
  topbarItems: [NotificationBell],
  routes: [
    {
      path: 'notifications/templates',
      lazy: async () => ({ Component: (await import('./pages/TemplatesPage')).TemplatesPage }),
      handle: { permission: 'notifications.templates.manage', titleKey: 'nav.notificationTemplates' },
    },
  ],
};

export default manifest;
