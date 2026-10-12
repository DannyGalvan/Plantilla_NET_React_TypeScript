/// <reference types="vitest/config" />
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { env } from 'node:process';
import { fileURLToPath, URL } from 'node:url';

import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig, type ServerOptions } from 'vite';

/**
 * Development HTTPS uses the ASP.NET Core development certificate (exported once with `dotnet dev-certs`), so the
 * SPA and the API share a trusted certificate. Builds and tests never touch it.
 */
function devHttps(): ServerOptions['https'] {
  const folder = env.APPDATA ? join(env.APPDATA, 'ASP.NET', 'https') : join(env.HOME ?? '.', '.aspnet', 'https');
  const cert = join(folder, 'project.client.pem');
  const key = join(folder, 'project.client.key');

  if (!existsSync(cert) || !existsSync(key)) {
    mkdirSync(folder, { recursive: true });
    const result = spawnSync(
      'dotnet',
      ['dev-certs', 'https', '--export-path', cert, '--format', 'Pem', '--no-password'],
      {
        stdio: 'inherit',
      },
    );
    if (result.status !== 0) {
      console.warn('Could not export the ASP.NET development certificate; Vite will use HTTP.');
      return undefined;
    }
  }

  return { cert: readFileSync(cert), key: readFileSync(key) };
}

const apiTarget = env.API_URL ?? 'https://localhost:7180';

export default defineConfig(({ command }) => ({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    strictPort: true,
    https: command === 'serve' && env.VITE_HTTPS !== 'false' ? devHttps() : undefined,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true, secure: false },
      '/health': { target: apiTarget, changeOrigin: true, secure: false },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    chunkSizeWarningLimit: 900,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    include: ['src/**/*.test.{ts,tsx}'],
    coverage: {
      provider: 'v8',
      include: ['src/shared/**/*.{ts,tsx}'],
      exclude: ['src/shared/ui/**', 'src/**/*.test.{ts,tsx}'],
    },
  },
}));
