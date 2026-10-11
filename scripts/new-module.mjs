#!/usr/bin/env node
/**
 * Scaffolds a new frontend module under `src/modules/<name>`.
 *
 * Usage:
 *   node scripts/new-module.mjs inventory
 *
 * Creates:
 *   src/modules/inventory/module.ts              (ModuleManifest default export)
 *   src/modules/inventory/api/inventory-api.ts    (Axios CRUD wrapper)
 *   src/modules/inventory/schemas/inventory.ts    (zod input/output schemas)
 *   src/modules/inventory/pages/InventoryPage.tsx (placeholder page)
 *   src/modules/inventory/i18n/es.json
 *   src/modules/inventory/i18n/en.json
 */
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const name = process.argv[2];
if (!name) {
  console.error('Usage: node scripts/new-module.mjs <module-name>');
  process.exit(1);
}

const pascal = name[0].toUpperCase() + name.slice(1);
const kebab = name.toLowerCase();
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const base = join(root, 'src', 'modules', kebab);

await mkdir(join(base, 'api'), { recursive: true });
await mkdir(join(base, 'pages'), { recursive: true });
await mkdir(join(base, 'schemas'), { recursive: true });
await mkdir(join(base, 'i18n'), { recursive: true });

await writeFile(join(base, 'module.ts'), `import type { ModuleManifest } from '../../app/module-registry';

export default {
  id: '${kebab}',
  routes: [
    {
      path: '/${kebab}',
      lazy: () => import('./pages/${pascal}Page'),
    },
  ],
  nav: [
    {
      key: '${kebab}',
      labelKey: '${kebab}.nav.label',
      icon: 'package',
      to: '/${kebab}',
      order: 50,
    },
  ],
  i18n: {
    es: () => import('./i18n/es.json'),
    en: () => import('./i18n/en.json'),
  },
} satisfies ModuleManifest;
`);

await writeFile(join(base, 'api', `${kebab}-api.ts`), `import { z } from 'zod';

import { env } from '../../../shared/config/env';

const base = env.VITE_API_URL;

export const ${kebab}InputSchema = z.object({
  name: z.string().min(1, 'Requerido'),
});

export type ${pascal}Input = z.infer<typeof ${kebab}InputSchema>;

export interface ${pascal}Dto {
  id: string;
  name: string;
}

export async function list${pascal}(): Promise<${pascal}Dto[]> {
  const res = await fetch(\`\${base}/${kebab}\`, { credentials: 'include' });
  if (!res.ok) throw new Error(\`list${pascal} \${res.status}\`);
  return (await res.json()) as ${pascal}Dto[];
}
`);

await writeFile(join(base, 'schemas', `${kebab}.ts`), `export interface ${pascal} {
  id: string;
  name: string;
}
`);

await writeFile(join(base, 'pages', `${pascal}Page.tsx`), `import { Card, CardContent, CardHeader, CardTitle } from '../../../shared/ui/card';
import { useTranslation } from '../../../shared/i18n/use-translation';

export function ${pascal}Page() {
  const { t } = useTranslation();
  return (
    <main style={{ padding: '2rem' }}>
      <Card>
        <CardHeader>
          <CardTitle>{t('${kebab}.nav.label')}</CardTitle>
        </CardHeader>
        <CardContent>
          <p>{t('${kebab}.empty')}</p>
        </CardContent>
      </Card>
    </main>
  );
}
`);

await writeFile(join(base, 'i18n', 'es.json'), JSON.stringify({
  [`${kebab}.nav.label`]: pascal,
  [`${kebab}.empty`]: 'Aún no hay elementos.',
}, null, 2) + '\n');

await writeFile(join(base, 'i18n', 'en.json'), JSON.stringify({
  [`${kebab}.nav.label`]: pascal,
  [`${kebab}.empty`]: 'No items yet.',
}, null, 2) + '\n');

console.log(`Scaffolded module '${kebab}' under src/modules/${kebab}`);
console.log('Next steps:');
console.log(`  1. Wire the new module's routes via getAllManifests() in app/module-registry.ts.`);
console.log(`  2. Define the server-side CRUD endpoints (or import the generator script).`);
console.log(`  3. Add the permission keys to your Contracts project's Permissions.cs.`);
`);

process.exit(0);