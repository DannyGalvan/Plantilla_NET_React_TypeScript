import tsconfigPaths from "vite-tsconfig-paths";
import { defineConfig } from "vitest/config";

/**
 * Vitest config — Phase 5.2.
 *
 * - jsdom environment for React Testing Library.
 * - globals: true so @testing-library/jest-dom matchers attach to the
 *   global `expect` (see src/test/setup.ts).
 * - css: false — we don't render CSS in unit tests.
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
  },
});
