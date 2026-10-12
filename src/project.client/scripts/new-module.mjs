#!/usr/bin/env node
// Scaffolds a frontend module that talks to a backend resource created with MapCrud.
//
// Usage: pnpm new-module <module> [resource]
// Example: pnpm new-module inventory warehouses
//   -> src/modules/inventory/{module.ts,api.ts,i18n.ts,pages/WarehousesPage.tsx}
//   -> route /inventory/warehouses protected by inventory.warehouses.read
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const [moduleName, resourceArg] = process.argv.slice(2);
if (!moduleName || !/^[a-z][a-z0-9-]*$/.test(moduleName)) {
    console.error('Usage: pnpm new-module <module> [resource]   (lower case, for example: inventory warehouses)');
    process.exit(1);
}

const resource = resourceArg ?? 'items';
const pascal = (value) => value.replace(/(^|-)([a-z])/g, (_, __, letter) => letter.toUpperCase());
const Resource = pascal(resource);
const dir = join(new URL('../src/modules', import.meta.url).pathname, moduleName);

if (existsSync(dir)) {
    console.error(`The module folder already exists: ${dir}`);
    process.exit(1);
}

mkdirSync(join(dir, 'pages'), { recursive: true });

writeFileSync(
    join(dir, 'api.ts'),
    `import { z } from 'zod';

import { createCrudApi } from '@/shared/api/crud-api';

/** Mirror of the backend DTO. Types are always z.infer of the schema. */
export const ${resource.replace(/-/g, '')}Schema = z.object({
  id: z.string(),
  name: z.string(),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

export type ${Resource}Item = z.infer<typeof ${resource.replace(/-/g, '')}Schema>;

export interface ${Resource}Input {
  name: string;
  concurrencyStamp?: string;
}

export const ${resource.replace(/-/g, '')}Api = createCrudApi<typeof ${resource.replace(/-/g, '')}Schema, ${Resource}Input, ${Resource}Input>('/${moduleName}/${resource}', ${resource.replace(/-/g, '')}Schema);
`,
);

writeFileSync(
    join(dir, 'i18n.ts'),
    `import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: { ${resource.replace(/-/g, '')}: { title: '${Resource}', description: 'Administra ${resource}.' } },
  en: { ${resource.replace(/-/g, '')}: { title: '${Resource}', description: 'Manage ${resource}.' } },
};
`,
);

writeFileSync(
    join(dir, 'pages', `${Resource}Page.tsx`),
    `import type { ColumnDef } from '@tanstack/react-table';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

import { useCrudList } from '@/shared/api/crud-api';
import { useQueryScope } from '@/shared/api/query-keys';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';

import { ${resource.replace(/-/g, '')}Api, type ${Resource}Item } from '../api';

export function ${Resource}Page() {
  const { t } = useTranslation('${moduleName}');
  const scope = useQueryScope('${moduleName}', '${resource}');
  const [state, setState] = useTableState({ sort: 'name' });
  const list = useCrudList(scope, ${resource.replace(/-/g, '')}Api, toListParams(state, (q) => \`name:contains:\${q}\`));

  const columns = useMemo<ColumnDef<${Resource}Item, unknown>[]>(
    () => [
      { accessorKey: 'name', header: t('common:common.name'), meta: { sortField: 'name' } },
      { accessorKey: 'createdAt', header: t('common:common.createdAt'), cell: ({ row }) => formatDate(row.original.createdAt) },
    ],
    [t],
  );

  return (
    <div className="space-y-6">
      <PageHeader title={t('${resource.replace(/-/g, '')}.title')} description={t('${resource.replace(/-/g, '')}.description')} />
      <DataTable id="${moduleName}-${resource}" columns={columns} data={list.data} isLoading={list.isLoading} error={list.error} state={state} onStateChange={setState} />
    </div>
  );
}
`,
);

writeFileSync(
    join(dir, 'module.ts'),
    `import type { ModuleManifest } from '@/shared/modules/manifest';

import { resources } from './i18n';

const manifest: ModuleManifest = {
  id: '${moduleName}',
  backendModule: '${moduleName}',
  i18n: resources,
  routes: [
    {
      path: '${moduleName}/${resource}',
      lazy: async () => ({ Component: (await import('./pages/${Resource}Page')).${Resource}Page }),
      handle: { permission: '${moduleName}.${resource}.read', titleKey: '${moduleName}:${resource.replace(/-/g, '')}.title' },
    },
  ],
};

export default manifest;
`,
);

try {
    execFileSync('pnpm', ['exec', 'prettier', '--write', dir], {
        cwd: new URL('..', import.meta.url).pathname,
        stdio: 'ignore',
    });
} catch {
    console.warn('Prettier could not format the new files; run pnpm format.');
}

console.log(`OK: module '${moduleName}' created in ${dir}.`);
console.log(
    `Declare the navigation entry on the backend (IPermissionProvider.GetNavigation) with path /${moduleName}/${resource}.`,
);
