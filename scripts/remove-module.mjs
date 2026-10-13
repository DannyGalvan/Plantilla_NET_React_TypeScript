#!/usr/bin/env node
// Removes a full-stack module: its four backend projects, its migrations and design-time factories on the three
// engines, its registration (solution, AppModules, project references) and its frontend folder. The inverse of
// new-module.mjs. Review the result with `git status` and `git diff` before committing.
//
// Usage:   node scripts/remove-module.mjs <Module>
// Example: node scripts/remove-module.mjs Catalog
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const [Module] = process.argv.slice(2);
const core = ['Tenancy', 'Identity', 'Organization'];
if (!Module || !/^[A-Z][A-Za-z0-9]+$/.test(Module)) {
  console.error('Usage: node scripts/remove-module.mjs <Module>   (PascalCase, for example: Catalog)');
  process.exit(1);
}

if (core.includes(Module)) {
  console.error(`${Module} is a platform module. Disable it with Modules:Disabled instead of removing it.`);
  process.exit(1);
}

const root = resolve(new URL('..', import.meta.url).pathname);
const base = join(root, 'src', 'Modules', Module);
if (!existsSync(base)) {
  console.error(`The module does not exist: ${base}`);
  process.exit(1);
}

const escape = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const edit = (relative, transform) => {
  const path = join(root, relative);
  if (existsSync(path)) {
    writeFileSync(path, transform(readFileSync(path, 'utf8')));
  }
};
const remove = (relative) => rmSync(join(root, relative), { recursive: true, force: true });
const withoutReference = (s) => s.replace(new RegExp(`[ \\t]*<ProjectReference Include="[^"]*Project\\.Modules\\.${Module}\\.[^"]*" />\\r?\\n`, 'g'), '');

// ----- Backend projects and registration -----
remove(`src/Modules/${Module}`);
edit('Project.slnx', (s) => s.replace(new RegExp(`[ \\t]*<Folder Name="/src/Modules/${Module}/">[\\s\\S]*?</Folder>\\r?\\n`), ''));
edit('src/Host/Project.Composition/Project.Composition.csproj', withoutReference);
edit('tests/Project.UnitTests/Project.UnitTests.csproj', withoutReference);
edit('src/Host/Project.Composition/AppModules.cs', (s) =>
  s
    .replace(new RegExp(`using Project\\.Modules\\.${Module}\\.Infrastructure;\\r?\\n`), '')
    .replace(new RegExp(`[ \\t]*new ${Module}Module\\(\\),\\r?\\n`), ''),
);

// ----- Migrations on every engine -----
for (const provider of ['SqlServer', 'PostgreSql', 'MySql']) {
  const project = `src/Migrations/Project.Migrations.${provider}`;
  remove(`${project}/${Module}`);
  edit(`${project}/Project.Migrations.${provider}.csproj`, withoutReference);
  edit(`${project}/DesignTimeFactories.cs`, (s) =>
    s
      .replace(new RegExp(`using Project\\.Modules\\.${Module}\\.Infrastructure[^;]*;\\r?\\n`, 'g'), '')
      .replace(new RegExp(`\\r?\\npublic sealed class ${escape(Module)}DbContextFactory[\\s\\S]*?\\n}\\r?\\n`), '')
      .replace(/\s*$/, '\n'),
  );
}

// ----- Frontend -----
const client = `src/project.client/src/modules/${Module.toLowerCase()}`;
remove(client);

// ----- Leftovers: tests, docs or other modules that still mention it -----
let leftovers = '';
try {
  leftovers = execFileSync(
    'git',
    ['grep', '-n', '-I', '-i', '-E', `Modules\\.${Module}\\b|${Module}Module\\b|/${Module.toLowerCase()}/`, '--', 'src', 'tests', 'scripts', '.github'],
    { cwd: root, encoding: 'utf8' },
  );
} catch {
  // git grep exits with 1 when nothing matches.
}

console.log(`
OK: module ${Module} removed (backend, migrations, registration and frontend).`);
if (leftovers.trim()) {
  console.log(`
Review these references (tests, docs or code that used the module):
${leftovers}`);
}

console.log(`Next steps:
  1. dotnet build Project.slnx && dotnet test Project.slnx
  2. cd src/project.client && pnpm verify
  3. Existing databases keep the "${Module.toLowerCase()}" schema and tables: drop them manually if you no longer need the data.
`);
