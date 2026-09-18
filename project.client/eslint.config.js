import eslintPluginJsxA11y from "eslint-plugin-jsx-a11y";
import eslintPluginReactQuery from "@tanstack/eslint-plugin-query";
import eslintPluginImport from "eslint-plugin-import";
import pkg from "eslint-plugin-perfectionist";
import eslintPluginPrettier from "eslint-plugin-prettier";
import eslintPluginReact from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import eslintPluginTailwindCSS from "eslint-plugin-tailwindcss";
import eslintUnusedImports from "eslint-plugin-unused-imports";
import globals from "globals";
import tseslint from "typescript-eslint";

/**
 * Phase 5.1 — curated ESLint config.
 *
 * Goals (from agents.md §6.1):
 *   1. Replace the broad `*.all` spreads with targeted rule sets so the
 *      Prettier / format / a11y / security rules actually apply.
 *   2. De-duplicate keys (the old config redefined `react/jsx-indent` twice,
 *      `react/jsx-one-expression-per-line` twice, `react/jsx-tag-spacing`
 *      twice).
 *   3. Activate the previously registered-but-unused plugins:
 *      `unused-imports`, `perfectionist`, `@tanstack/eslint-plugin-query`,
 *      and the never-referenced `jsx-a11y`.
 *   4. Promote the security-sensitive rules to `error` (closes F7).
 */
export default tseslint.config(
  { ignores: ["dist", "coverage", "node_modules"] },
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
      prettier: eslintPluginPrettier,
      react: eslintPluginReact,
      "unused-imports": eslintUnusedImports,
      import: eslintPluginImport,
      tailwindcss: eslintPluginTailwindCSS,
      "react-query": eslintPluginReactQuery,
      perfectionist: pkg,
      "jsx-a11y": eslintPluginJsxA11y,
      "@tanstack/query": eslintPluginReactQuery,
    },
    settings: {
      react: { version: "detect" },
    },
    rules: {
      // ---- React -----------------------------------------------------------
      ...(reactHooks.configs?.recommended?.rules ?? {}),
      ...(eslintPluginReact.configs?.recommended?.rules ?? {}),
      "react/react-in-jsx-scope": "off",
      "react/jsx-filename-extension": [
        "error",
        { extensions: [".jsx", ".tsx"] },
      ],
      "react/jsx-no-literals": "off",
      "react/require-default-props": "off",
      "react/forbid-component-props": "off",
      "react/jsx-max-depth": ["warn", { max: 6 }],
      "react/jsx-sort-props": [
        "warn",
        {
          callbacksLast: true,
          shorthandFirst: true,
          noSortAlphabetically: false,
          reservedFirst: true,
        },
      ],

      // ---- Tailwind --------------------------------------------------------
      ...(eslintPluginTailwindCSS.configs?.recommended?.rules ?? {}),

      // ---- Import ----------------------------------------------------------
      // `import/no-unresolved` requires the TS path resolver (the project
      // uses vite-tsconfig-paths at runtime, not ESLint). Disable to avoid
      // false positives on TS aliases like `@/...` or relative `../`.
      "import/no-unresolved": "off",
      "import/order": [
        "warn",
        { "newlines-between": "always", alphabetize: { order: "asc" } },
      ],

      // ---- TanStack Query --------------------------------------------------
      ...(eslintPluginReactQuery.configs?.recommended?.rules ?? {}),

      // ---- Perfectionist (sorting) ----------------------------------------
      ...(pkg.configs?.recommended?.rules ?? {}),

      // ---- a11y (activated now) -------------------------------------------
      ...(eslintPluginJsxA11y.configs?.recommended?.rules ?? {}),

      // ---- Unused imports --------------------------------------------------
      "unused-imports/no-unused-imports": "error",
      "unused-imports/no-unused-vars": [
        "warn",
        {
          vars: "all",
          varsIgnorePattern: "^_",
          args: "none",
        },
      ],

      // ---- Prettier --------------------------------------------------------
      ...(eslintPluginPrettier.configs?.recommended?.rules ?? {}),

      // ---- React Refresh ---------------------------------------------------
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true },
      ],

      // ---- Security-sensitive (F7) ----------------------------------------
      "react/no-danger": "error",
      "react/jsx-no-script-url": "error",
      "react/jsx-no-target-blank": "error",
      "no-eval": "error",
      "no-implied-eval": "error",
      "no-script-url": "error",

      // ---- Disabled noisy rules ------------------------------------------
      "arrow-body-style": "off",
      "prefer-arrow-callback": "off",
      "max-len": "off",
    },
  },
);