#!/usr/bin/env node
// Fails when components use raw colors instead of design tokens.
// Allowed: semantic utilities (bg-primary, text-muted-foreground...). Forbidden outside src/shared/theme:
//   - raw Tailwind palettes: bg-red-500, text-slate-700, border-zinc-200...
//   - hex colors: #fff, #1d4ed8
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const root = new URL('../src', import.meta.url).pathname;
const palettes =
    'slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose';
const rawPalette = new RegExp(
    `\\b(?:bg|text|border|ring|from|to|via|fill|stroke|outline|decoration|divide|shadow|accent|caret)-(?:${palettes})-\\d{2,3}\\b`,
    'g',
);
const hexColor = /#[0-9a-fA-F]{3,8}\b/g;

function* files(dir) {
    for (const entry of readdirSync(dir)) {
        const path = join(dir, entry);
        if (statSync(path).isDirectory()) {
            yield* files(path);
        } else if (/\.(tsx?|css)$/.test(entry)) {
            yield path;
        }
    }
}

const problems = [];
for (const file of files(root)) {
    const rel = relative(root, file);
    if (rel.startsWith(join('shared', 'theme')) || rel.endsWith('.test.ts') || rel.endsWith('.test.tsx')) {
        continue;
    }

    const lines = readFileSync(file, 'utf8').split('\n');
    lines.forEach((line, index) => {
        for (const match of [...line.matchAll(rawPalette), ...line.matchAll(hexColor)]) {
            // Allow hex values used as color input defaults and example placeholders inside strings marked as such.
            if (line.includes('tokens-allow')) {
                continue;
            }

            problems.push(`${rel}:${index + 1}  ${match[0]}`);
        }
    });
}

if (problems.length > 0) {
    console.error('Raw colors found. Use the semantic tokens of src/shared/theme/tokens.css instead:\n');
    console.error(problems.map((problem) => `  ${problem}`).join('\n'));
    process.exit(1);
}

console.log('OK: no raw palette classes or hex colors outside src/shared/theme.');
