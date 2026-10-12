#!/usr/bin/env node
// Documentation checks used by CI:
//   1. No emojis in Markdown (professional documentation standard of the template).
//   2. Every relative link in Markdown points to an existing file.
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';

const root = resolve(new URL('..', import.meta.url).pathname);
const ignored = new Set(['node_modules', '.git', 'bin', 'obj', 'dist', 'graphify-out', 'plans', '.claude', 'TestResults']);
const emoji = /\p{Extended_Pictographic}/u;
const link = /\]\(([^)\s]+)\)/g;

function* markdown(dir) {
  for (const entry of readdirSync(dir)) {
    if (ignored.has(entry)) continue;
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) yield* markdown(path);
    else if (entry.endsWith('.md') && entry !== 'CLAUDE.md') yield path;
  }
}

const problems = [];
for (const file of markdown(root)) {
  const rel = relative(root, file);
  readFileSync(file, 'utf8')
    .split('\n')
    .forEach((line, index) => {
      if (emoji.test(line)) problems.push(`${rel}:${index + 1}  emoji found`);
      for (const [, target] of line.matchAll(link)) {
        if (/^(https?:|mailto:|#)/.test(target)) continue;
        const path = target.split('#')[0];
        if (path && !existsSync(resolve(dirname(file), decodeURIComponent(path)))) {
          problems.push(`${rel}:${index + 1}  broken link ${target}`);
        }
      }
    });
}

if (problems.length) {
  console.error(problems.join('\n'));
  process.exit(1);
}

console.log('OK: documentation has no emojis and no broken relative links.');
