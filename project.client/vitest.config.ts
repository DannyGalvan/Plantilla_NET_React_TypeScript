import tsconfigPaths from "vite-tsconfig-paths";
import { defineConfig } from "vitest/config";

/**
 * Vitest config — Phase 5.2.
 *
 * - jsdom environment for React Testing Library.
 * - globals: false (so tests must import `describe`/`it`/`expect` explicitly).
 * - setupFiles: @testing-library/jest-dom matchers.
 * - vite-tsconfig-paths so path aliases resolve identically to the SPA build.
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
