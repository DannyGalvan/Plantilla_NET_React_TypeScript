import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Library, attachments and storage connections (Google Drive, OneDrive, S3, Azure Blob). */
const manifest: ModuleManifest = {
  id: 'files',
  backendModule: 'files',
  i18n: resources,
  routes: [
    {
      path: 'files',
      lazy: async () => ({ Component: (await import('./pages/FilesPage')).FilesPage }),
      handle: { permission: 'files.library.read', titleKey: 'nav.files' },
    },
    {
      path: 'files/storage',
      lazy: async () => ({ Component: (await import('./pages/StoragePage')).StoragePage }),
      handle: { permission: 'files.storage.manage', titleKey: 'nav.storage' },
    },
  ],
};

export default manifest;
