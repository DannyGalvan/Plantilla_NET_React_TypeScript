import tsconfigPaths from "vite-tsconfig-paths";
import { defineConfig } from "vitest/config";

/**
 * Vitest config — Phase 5.2.
 *
 * - jsdom environment for React Testing Library.
 * - server.deps.inline forces Vite to bundle jsdom + undici + webidl-
 *   conversions. Without this, pnpm's hoisted layout can leave undici
 *   resolving webidl-conversions from a copy that doesn't expose
 *   `markAsUncloneable`, which crashes every test that touches the
 *   network stack (axios interceptor tests, ProtectedRoute with
 *   MemoryRouter, etc.).
 * - globals: true so @testing-library/jest-dom matchers attach to the
 *   global `expect`.
 * - vite-tsconfig-paths so `@/...` aliases resolve the same way they
 *   do in the production build.
 */
export default defineConfig({
  plugins: [tsconfigPaths()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    css: false,
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    server: {
      deps: {
        inline: ["jsdom", "undici", "webidl-conversions"],
      },
    },
  },
});
