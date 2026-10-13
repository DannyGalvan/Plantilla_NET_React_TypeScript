import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

/** Formats of the document sequences declared by the modules. */
const manifest: ModuleManifest = {
  id: 'sequences',
  backendModule: 'sequences',
  i18n: resources,
  routes: [
    {
      path: 'sequences',
      lazy: async () => ({ Component: (await import('./pages/SequencesPage')).SequencesPage }),
      handle: { permission: 'sequences.sequences.manage', titleKey: 'nav.sequences' },
    },
  ],
};

export default manifest;
