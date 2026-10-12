import type { ModuleManifest } from '@/shared/modules/manifest';

import { catalogResources } from './i18n';

/** Sample business module. Copy it (or run `pnpm new-module <name>`) to start a new one. */
const manifest: ModuleManifest = {
  id: 'catalog',
  backendModule: 'catalog',
  i18n: catalogResources,
  routes: [
    {
      path: 'catalog/products',
      lazy: async () => ({ Component: (await import('./pages/ProductsPage')).ProductsPage }),
      handle: { permission: 'catalog.products.read', titleKey: 'nav.products' },
    },
    {
      path: 'catalog/categories',
      lazy: async () => ({ Component: (await import('./pages/CategoriesPage')).CategoriesPage }),
      handle: { permission: 'catalog.categories.read', titleKey: 'nav.categories' },
    },
    {
      path: 'catalog/reports/stock',
      lazy: async () => ({ Component: (await import('./pages/StockReportPage')).StockReportPage }),
      handle: { permission: 'catalog.reports.read', titleKey: 'nav.stockReport' },
    },
  ],
};

export default manifest;
